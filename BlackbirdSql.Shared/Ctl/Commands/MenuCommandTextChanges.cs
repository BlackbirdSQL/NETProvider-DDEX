﻿#region Assembly Microsoft.VisualStudio.Data.Tools.SqlEditor, Version=17.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
// location unknown
// Decompiled with ICSharpCode.Decompiler 7.1.0.6543
#endregion

using System;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using BlackbirdSql.Shared.Interfaces;



namespace BlackbirdSql.Shared.Ctl.Commands;

[ComVisible(false)]


public class MenuCommandTextChanges : MenuCommand, IBMenuCommandTextChanges
{

	public MenuCommandTextChanges(EventHandler handler, CommandID command)
		: base(handler, command)
	{
	}


	private string _Text;


	public string Text
	{
		get { return _Text; }
		set { _Text = value; }
	}
}
