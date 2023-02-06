﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BlackbirdSql.Common.Extensions
{
	static class ExtensionMethods
	{
		/// <summary>
		/// Mimicks the search performed by assignment to <see cref="ListControl.SelectedValue"/> where no <see cref="ListControl.ValueMember"/> exists and
		/// returns the assigned object so that inline assignments in series can also cast the object.
		/// eg.string str = (string)combo.SetSelectedValueX(obj);
		/// </summary>
		/// <param name="comboBox"></param>
		/// <param name="value"></param>
		/// <returns>The new found <see cref="ComboBox.Items"/> value at the new <see cref="ComboBox.SelectedIndex"/> else null</returns>
		public static object SetSelectedValueX(this ComboBox comboBox, object value)
		{
			if (value == null)
			{
				comboBox.SelectedIndex = -1;
				return null;
			}
			
			int result;

			if (value is string str)
				result = comboBox.FindStringExact(str);
			else
				result = comboBox.FindStringExact((string)value);

			comboBox.SelectedIndex = result;

			return result == -1 ? null : value;
		}


		/// <summary>
		/// Allows casting of <see cref="ComboBox.SelectedIndex"/> when doing multiple assignments in series.
		/// eg. string x = cbo.SetSelectedIndexX(index).ToString();
		/// </summary>
		/// <param name="comboBox"></param>
		/// <param name="index"></param>
		/// <returns>The new value of <see cref="ComboBox.SelectedIndex"/> else -1.</returns>
		public static int SetSelectedIndexX(this ComboBox comboBox, int index)
		{
			comboBox.SelectedIndex = index;

			return comboBox.SelectedIndex;
		}
	}
}