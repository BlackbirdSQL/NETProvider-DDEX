﻿// Microsoft.SqlServer.ConnectionDlg.Core, Version=17.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
// Microsoft.SqlServer.ConnectionDlg.Core.ITrace

using BlackbirdSql.Core.Controls.Events;



namespace BlackbirdSql.Core.Ctl.Interfaces;


public interface IBEditConverter
{
	void OnEditControlGotFocus(object sender, EditControlFocusEventArgs e);
	void OnEditControlLostFocus(object sender, EditControlFocusEventArgs e);
}
