﻿using System;

namespace SlimTuneUI
{
	public class DummyStorageEngine : UICore.IStorageEngine
	{
		public string Name
		{
			get { return "Dummy Engine"; }
		}

		public string Extension
		{
			get { return "*"; }
		}

		public string Engine
		{
			get { return "Dummy"; }
		}

		public bool InMemory
		{
			get { return true; }
		}

		public void MapFunction(UICore.FunctionInfo funcInfo)
		{
		}

		public void MapClass(UICore.ClassInfo classInfo)
		{
		}

		public void ParseSample(UICore.Messages.Sample sample)
		{
		}

		public void ClearData()
		{
		}

		public void UpdateThread(int threadId, bool? alive, string name)
		{
		}

		public void FunctionTiming(int functionId, long time)
		{
		}

		public void CounterName(int counterId, string name)
		{
		}

		public void PerfCounter(int counterId, long time, long value)
		{
		}

		public bool AllowFlush
		{
			get
			{
				return true;
			}
			set
			{
			}
		}

		public void Flush()
		{
		}

		public void Snapshot(string name)
		{
		}

		public void Save(string file)
		{
		}

		public System.Data.DataSet Query(string query)
		{
			throw new NotImplementedException();
		}

		public System.Data.DataSet Query(string query, int limit)
		{
			throw new NotImplementedException();
		}

		public object QueryScalar(string query)
		{
			throw new NotImplementedException();
		}

		#region IDisposable Members

		public void Dispose()
		{
		}

		#endregion
	}
}