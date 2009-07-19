﻿/*
* Copyright (c) 2009 SlimDX Group
* All rights reserved. This program and the accompanying materials
* are made available under the terms of the Eclipse Public License v1.0
* which accompanies this distribution, and is available at
* http://www.eclipse.org/legal/epl-v10.html
*/
using System;
using System.IO;
using System.Collections.Generic;
/*
* Copyright (c) 2009 SlimDX Group
* All rights reserved. This program and the accompanying materials
* are made available under the terms of the Eclipse Public License v1.0
* which accompanies this distribution, and is available at
* http://www.eclipse.org/legal/epl-v10.html
*/
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.Threading;
using System.Data.SqlServerCe;

namespace SlimTuneUI
{
	public partial class Results : WeifenLuo.WinFormsUI.Docking.DockContent
	{
		private const string ProfilerGuid_x86 = "{38A7EA35-B221-425a-AD07-D058C581611D}";

		Thread m_recvThread;
		SqlCeConnection m_sqlConn;
		volatile bool m_receive = false;

		public Results()
		{
			InitializeComponent();
		}

		private static void CreateSchema(SqlCeConnection conn)
		{
			var createMapTable = new SqlCeCommand("CREATE TABLE Mappings (Id INT NOT NULL, Name NVARCHAR (1024))", conn);
			createMapTable.ExecuteNonQuery();

			var createCallersTable = new SqlCeCommand("CREATE TABLE Callers (ThreadId INT NOT NULL, CallerId INT NOT NULL, CalleeId INT NOT NULL, HitCount INT)", conn);
			createCallersTable.ExecuteNonQuery();
			var addCallersKey = new SqlCeCommand("ALTER TABLE Callers ADD CONSTRAINT PK_Callers PRIMARY KEY (ThreadId, CallerId, CalleeId)", conn);
			addCallersKey.ExecuteNonQuery();

			//note that Caller and Callee are reversed here
			var createCalleesTable = new SqlCeCommand("CREATE TABLE Callees (ThreadId INT NOT NULL, CalleeId INT NOT NULL, CallerId INT NOT NULL, HitCount INT)", conn);
			createCalleesTable.ExecuteNonQuery();
			var addCalleesKey = new SqlCeCommand("ALTER TABLE Callees ADD CONSTRAINT PK_Callees PRIMARY KEY (ThreadId, CalleeId, CallerId)", conn);
			addCalleesKey.ExecuteNonQuery();
		}

		public bool LaunchLocal(string exe, string args, string dbFile)
		{
			if(!File.Exists(exe))
				return false;

			string connStr = "Data Source='" + dbFile + "'; LCID=1033;";
			try
			{
				if(File.Exists(dbFile))
					File.Delete(dbFile);

				SqlCeEngine engine = new SqlCeEngine(connStr);
				engine.CreateDatabase();
			}
			catch
			{
				return false;
			}

			m_sqlConn = new SqlCeConnection(connStr);
			m_sqlConn.Open();
			CreateSchema(m_sqlConn);

			var psi = new ProcessStartInfo(exe, args);
			psi.UseShellExecute = false;

			if(psi.EnvironmentVariables.ContainsKey("COR_ENABLE_PROFILING") == true)
				psi.EnvironmentVariables["COR_ENABLE_PROFILING"] = "1";
			else
				psi.EnvironmentVariables.Add("COR_ENABLE_PROFILING", "1");

			if(psi.EnvironmentVariables.ContainsKey("COR_PROFILER") == true)
				psi.EnvironmentVariables["COR_PROFILER"] = ProfilerGuid_x86;
			else
				psi.EnvironmentVariables.Add("COR_PROFILER", ProfilerGuid_x86);

			Process.Start(psi);

			//TODO: select host/port
			ProfilerClient client = null;
			for(int i = 0; i < 5; ++i)
			{
				try
				{
					client = new ProfilerClient("localhost", 200, m_sqlConn);
				}
				catch(System.Net.Sockets.SocketException)
				{
					Thread.Sleep(500);
					continue;
				}

				break;
			}

			if(client == null)
				return false;

			m_recvThread = new Thread(new ParameterizedThreadStart(ReceiveThread));
			m_receive = true;
			m_recvThread.Start(client);
			m_updateTimer.Enabled = true;
			return true;
		}

		public void ReceiveThread(object data)
		{
			var client = (ProfilerClient) data;

			try
			{
				while(m_receive)
				{
					try
					{
						string text = client.Receive();
						if(text == null)
							break;
					}
					catch
					{
						m_receive = false;
					}
				}
			}
			finally
			{
				client.Dispose();
				this.Invoke((MethodInvoker) delegate { m_updateTimer.Enabled = false; });
			}
		}

		private void Results_FormClosing(object sender, FormClosingEventArgs e)
		{
			if(m_receive && m_recvThread.ThreadState == System.Threading.ThreadState.Running)
			{
				DialogResult result = MessageBox.Show("Closing this window will disconnect the profiler. Close anyway?",
					"Profiler Still Connected", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
				if(result == DialogResult.No)
					e.Cancel = true;
			}

			if(!e.Cancel)
			{
				m_receive = false;
				if(m_recvThread != null)
					m_recvThread.Join();

				m_updateTimer.Enabled = false;
				m_sqlConn.Close();
			}
		}

		private void m_updateTimer_Tick(object sender, EventArgs e)
		{
			var cmd = new SqlCeCommand("SELECT COUNT(*) FROM Mappings", m_sqlConn);
			int count = (int) cmd.ExecuteScalar();
			m_mappingCountLabel.Text = "Mappings: " + count;

			var getMappings = new SqlCeCommand("SELECT * FROM Mappings", m_sqlConn);
			var adapter = new SqlCeDataAdapter(getMappings);
			var ds = new DataSet();
			adapter.Fill(ds, "Mappings");
			m_dataGrid.DataSource = ds;
			m_dataGrid.DataMember = "Mappings";
		}
	}
}