﻿// Microsoft.VisualStudio.Data.Tools.SqlEditor, Version=17.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
// Microsoft.VisualStudio.Data.Tools.SqlEditor.VSIntegration.SqlResultsEditorFactory

using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using BlackbirdSql.EditorExtension.Properties;
using BlackbirdSql.Shared.Controls;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;

using LibraryData = BlackbirdSql.Shared.LibraryData;



namespace BlackbirdSql.EditorExtension;

[Guid(LibraryData.C_ResultsEditorFactoryGuid)]
[ProvideMenuResource("Menus.ctmenu", 1)]


// =========================================================================================================
//
//											EditorFactoryResults Class
//
/// <summary>
/// Results Editor Factory.
/// </summary>
// =========================================================================================================
public sealed class EditorFactoryResults : AbstruseEditorFactory
{

	// ------------------------------------------------------
	#region Constructors / Destructors - EditorFactoryResults
	// ------------------------------------------------------


	public EditorFactoryResults() : base(encoded: false)
	{
	}



	public override int CreateEditorInstance(uint createFlags, string moniker, string physicalViewName,
		IVsHierarchy hierarchy, uint itemId, IntPtr pExistingDocData, out IntPtr pDocView,
		out IntPtr pDocData, out string caption, out Guid cmdUIGuid, out int hresult)
	{
		// RctManager.EnsureLoaded();

		pDocView = IntPtr.Zero;
		pDocData = IntPtr.Zero;
		caption = "";
		cmdUIGuid = Guid.Empty;
		hresult = VSConstants.S_FALSE;
		Cursor current = null;

		try
		{
			// Evs.Trace(GetType(), "IVsEditorFactory.CreateEditorInstance", "nCreateFlags = {0}, strMoniker = {1}, strPhysicalView = {2}, itemid = {3}", createFlags, moniker, physicalView, itemId);

			if (!string.IsNullOrEmpty(physicalViewName))
				return VSConstants.E_INVALIDARG;

			if ((createFlags & 6) == 0)
				return VSConstants.E_INVALIDARG;

			if (pExistingDocData != IntPtr.Zero)
				return VSConstants.VS_E_INCOMPATIBLEDOCDATA;

			current = Cursor.Current;
			Cursor.Current = Cursors.WaitCursor;

			hresult = VSConstants.S_OK;

			ResultPane resultWindowPane = CreateResultsWindowPane();
			caption = Resources.ResultsEditorFactory_Caption;
			pDocView = Marshal.GetIUnknownForObject(resultWindowPane);


			Guid clsid = typeof(VsTextBufferClass).GUID;
			Guid iid = VSConstants.IID_IUnknown;

			object objTextBuffer = EditorExtensionPackage.Instance.CreateInstance(ref clsid, ref iid, typeof(object));

			(objTextBuffer as IObjectWithSite)?.SetSite(OleServiceProvider);
			IVsTextLines vsTextLines = objTextBuffer as IVsTextLines;
			pDocData = Marshal.GetIUnknownForObject(vsTextLines);

			// pDocData = Marshal.GetIUnknownForObject(resultWindowPane); SqlEditor bug!!!

			cmdUIGuid = VSConstants.GUID_TextEditorFactory;
		}
		catch (Exception ex)
		{
			if (ex is NullReferenceException || ex is ApplicationException || ex is ArgumentException || ex is InvalidOperationException)
			{
				MessageCtl.ShowX(Resources.ExFailedToCreateEditor, ex);
				return VSConstants.E_FAIL;
			}

			throw;
		}
		finally
		{
			if (current != null)
				Cursor.Current = current;
		}

		return VSConstants.S_OK;
	}


	#endregion Constructors / Destructors





	// =========================================================================================================
	#region Property accessors - EditorFactoryResults
	// =========================================================================================================


	public override Guid ClsidEditorFactory => new(LibraryData.C_ResultsEditorFactoryGuid);


	#endregion Property accessors





	// =========================================================================================================
	#region Methods - EditorFactoryResults
	// =========================================================================================================


	public ResultPane CreateResultsWindowPane()
	{
		return new ResultPane();
	}


	#endregion Methods

}
