﻿using System;
using Microsoft.VisualStudio.Data.Services;


namespace BlackbirdSql.Core.Interfaces;

public interface IBRctEventSink : IDisposable
{
	bool Initialized { get; }
}
