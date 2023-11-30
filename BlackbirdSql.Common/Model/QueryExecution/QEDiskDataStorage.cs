// Microsoft.VisualStudio.Data.Tools.SqlEditor, Version=17.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
// Microsoft.VisualStudio.Data.Tools.SqlEditor.QueryExecution.QEDiskDataStorage
using System;
using BlackbirdSql.Common.Controls.Grid;
using BlackbirdSql.Common.Ctl;
using System.Data;
using BlackbirdSql.Common.Model.Interfaces;
using BlackbirdSql.Common.Properties;
using BlackbirdSql.Core.Ctl.Diagnostics;
using System.IO;

namespace BlackbirdSql.Common.Model.QueryExecution;

public class QEDiskDataStorage : AbstractDiskDataStorage, IBQEStorage, IBDataStorage, IDisposable
{
	private bool _IsClosed = true;


	public QEDiskDataStorage()
	{
		// Tracer.Trace(GetType(), "QEDiskDataStorage.QEDiskDataStorage", "", null);
		_DataStorageEnabled = true;
	}

	public virtual void InitStorage(IDataReader storageReader, bool textBased)
	{
		if (!textBased)
		{
			InitStorage(storageReader);
			return;
		}

		if (storageReader == null)
		{
			throw new Exception(ControlsResources.ReaderCannotBeNull);
		}
		_StorageReader = new StorageDataReader(storageReader, this);
		_FileName = Path.GetTempFileName();
		if (_FileName.Length == 0)
		{
			throw new Exception(ControlsResources.FailedToGetTempFileName);
		}
		_FsWriter = new FileStreamWriter();
		_FsWriter.Init(_FileName);

		for (int i = 0; i < this._StorageReader.FieldCount; i++)
		{
			ColumnInfo columnInfo = new ColumnInfo(this._StorageReader, i);
			_ColumnInfoArray.Add(columnInfo);
			_HasBlobs |= columnInfo.IsBlobField;
		}
	}

	public override void SerializeData()
	{
		// Tracer.Trace(GetType(), "QEDiskDataStorage.SerializeData", "", null);
		try
		{
			// Tracer.Trace(GetType(), Tracer.EnLevel.Verbose, "QEDiskDataStorage.SerializeData", "_DataStorageEnabled = {0}", _DataStorageEnabled);
			base.SerializeData();
		}
		catch (Exception e)
		{
			Tracer.LogExCatch(GetType(), e);
			long rowCount = RowCount;
			if (rowCount > 0)
			{
				OnStorageNotify(rowCount, storedAllData: true);
			}
			throw;
		}
		finally
		{
			_IsClosed = true;
		}
	}

	public new bool IsClosed()
	{
		return _IsClosed;
	}

	public new void StartStoringData()
	{
		if (!_IsClosed)
		{
			throw new InvalidOperationException(QEResources.ErrQEStorageAlreadyStoring);
		}
		_IsClosed = false;
		SerializeData();
	}

	public void InitiateStopStoringData()
	{
		_DataStorageEnabled = false;
	}

	public override IBStorageView GetStorageView()
	{
		// Tracer.Trace(GetType(), "QEDiskDataStorage.GetStorageView", "", null);
		QEDiskStorageView qEDiskStorageView = new QEDiskStorageView(this);
		if (MaxCharsToStore > 0)
		{
			qEDiskStorageView.MaxNumBytesToDisplay = MaxCharsToStore / 2;
		}
		return qEDiskStorageView;
	}
}
