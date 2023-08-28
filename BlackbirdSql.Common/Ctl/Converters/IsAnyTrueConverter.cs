// Microsoft.SqlServer.ConnectionDlg.UI, Version=17.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
// Microsoft.SqlServer.ConnectionDlg.UI.WPF.Converters.IsAnyTrueConverter
using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Data;

using BlackbirdSql.Core.Diagnostics;
using BlackbirdSql.Core.Diagnostics.Enums;




namespace BlackbirdSql.Common.Ctl.Converters;


[ValueConversion(typeof(bool[]), typeof(bool))]
public class IsAnyTrueConverter : IMultiValueConverter
{
	public virtual object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		UiTracer.TraceSource.AssertTraceEvent(targetType.IsAssignableFrom(typeof(bool)), TraceEventType.Error, EnUiTraceId.UiInfra, "TargetType must be typed bool");
		bool flag = false;
		if (values != null && values.Length != 0)
		{
			foreach (object obj in values)
			{
				if (obj is bool value && value)
				{
					flag = true;
					break;
				}
			}
		}
		return ConverterHelper.GetInvert(parameter) ? (!flag) : flag;
	}

	public virtual object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
	{
		UiTracer.TraceSource.AssertTraceEvent(condition: false, TraceEventType.Error, EnUiTraceId.UiInfra, "Not implemented");
		return new object[0];
	}
}