// Microsoft.VisualStudio.Data.Tools.SqlLanguageServices, Version=17.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
// Microsoft.VisualStudio.Data.Tools.SqlLanguageServices.Package
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using BlackbirdSql.Core;
using BlackbirdSql.LanguageExtension.Ctl.ComponentModel;
using BlackbirdSql.LanguageExtension.Ctl.Config;
using BlackbirdSql.LanguageExtension.Services;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.Win32;



namespace BlackbirdSql.LanguageExtension;


// =========================================================================================================
//										LanguageExtensionPackage Class 
//
/// <summary>
/// BlackbirdSql Language Extension <see cref="AsyncPackage"/> class implementation
/// </summary>
// =========================================================================================================



// ---------------------------------------------------------------------------------------------------------
#region							LanguageExtensionPackage Class Attributes
// ---------------------------------------------------------------------------------------------------------


[ProvideService(typeof(LsbLanguageService), IsAsyncQueryable = true, ServiceName = PackageData.LanguageServiceName)]
[ProvideLanguageService(typeof(LsbLanguageService), PackageData.LanguageLongName, 330, CodeSense = true, EnableCommenting = true, MatchBraces = true, ShowCompletion = true, ShowMatchingBrace = true, AutoOutlining = true, EnableAsyncCompletion = true, MaxErrorMessages = 200, CodeSenseDelay = 500)]

[VsProvideEditorAutomationPage(typeof(SettingsProvider.AdvancedPreferencesPage), SettingsProvider.CategoryName, "Advanced", 300, 330)]
[ProvideLanguageEditorOptionPage(typeof(SettingsProvider.AdvancedPreferencesPage), SettingsProvider.CategoryName, "Advanced", null, "#331")]
[ProvideProfile(typeof(SettingsProvider.AdvancedPreferencesPage), SettingsProvider.CategoryName, "Editor", 300, 330, false, AlternateParent = "AutomationProperties\\TextEditor")]

[ProvideLanguageExtension(typeof(LsbLanguageService), PackageData.Extension)]

[ProvideLanguageCodeExpansion(typeof(LsbLanguageService), "SQL_FIREBIRD", 303, "SQL_FIREBIRD", "%PackageFolder%\\Snippets\\SnippetsIndex.xml", SearchPaths = "%PackageFolder%\\Snippets\\Function;%PackageFolder%\\Snippets\\Index;%PackageFolder%\\Snippets\\Role;%PackageFolder%\\Snippets\\Stored Procedure;%PackageFolder%\\Snippets\\Table;%PackageFolder%\\Snippets\\Trigger;%PackageFolder%\\Snippets\\User;%PackageFolder%\\Snippets\\View;%MyDocs%\\Code Snippets\\SQL_FIREBIRD\\My Code Snippets", ForceCreateDirs = "%MyDocs%\\Code Snippets\\SQL_FIREBIRD\\My Code Snippets")]


#endregion Class Attributes



// =========================================================================================================
#region							LanguageExtensionPackage Class Declaration
// =========================================================================================================
public abstract class LanguageExtensionPackage : AbstractCorePackage, IOleComponent
{

	// ----------------------------------------------------------
	#region Constructors / Destructors - LanguageExtensionPackage
	// ----------------------------------------------------------


	public LanguageExtensionPackage() : base()
	{
		// SyncServiceContainer.AddService(typeof(LsbLanguageService), ServicesCreatorCallback, promote: true);
	}


	/// <summary>
	/// Gets the singleton Package instance
	/// </summary>
	public static new LanguageExtensionPackage Instance
	{
		get
		{
			if (_Instance == null)
				DemandLoadPackage(SystemData.AsyncPackageGuid, out _);
			return (LanguageExtensionPackage)_Instance;
		}
	}



	/// <summary>
	/// LanguageExtensionPackage package disposal
	/// </summary>
	protected override void Dispose(bool disposing)
	{
		try
		{
			if (_ComponentID != 0)
			{
				if (GetService(typeof(SOleComponentManager)) is IOleComponentManager oleComponentManager)
				{
					oleComponentManager.FRevokeComponent(_ComponentID);
				}
				_ComponentID = 0u;
			}
			UserDispose();
		}
		finally
		{
			base.Dispose(disposing);
		}
	}


	#endregion Constructors / Destructors




	// =========================================================================================================
	#region Constants & Fields - LanguageExtensionPackage
	// =========================================================================================================


	private uint _ComponentID;
	private LsbLanguageService _LanguageService;
	private LsbLanguagePreferences _UserPreferences = null;


	#endregion Constants & Fields





	// =========================================================================================================
	#region Property accessors - LanguageExtensionPackage
	// =========================================================================================================


	public static bool IsInitialized { get; private set; }

	public ITextUndoHistoryRegistry TextUndoHistoryRegistrySvc { get; private set; }

	public IVsEditorAdaptersFactoryService EditorAdaptersFactorySvc { get; private set; }





	public LsbLanguageService LanguageService
	{
		get
		{
			if (_LanguageService == null)
			{
				ThreadHelper.Generic.Invoke(delegate
				{
					GetService(typeof(LsbLanguageService));
				});
			}
			return _LanguageService;
		}
	}


	public bool UserPreferencesExist
	{
		get
		{
			using RegistryKey registryKey = UserRegistryRoot;

			string settingsKey = PackageData.RegistrySettingsKey;
			RegistryKey registryKey2 = registryKey.OpenSubKey(settingsKey, writable: false);

			return registryKey2 != null;
		}
	}


	#endregion Property accessors





	// =========================================================================================================
	#region Package Methods Implementations - LanguageExtensionPackage
	// =========================================================================================================



	// ---------------------------------------------------------------------------------
	/// <summary>
	/// Creates a service instance of the specified type if this class has access to the
	/// final class type of the service being added.
	/// The class requiring and adding the service may not necessarily be the class that
	/// creates an instance of the service.
	/// </summary>
	// ---------------------------------------------------------------------------------
	public override async Task<object> CreateServiceInstanceAsync(Type serviceType, CancellationToken token)
	{
		if (serviceType == null)
		{
			ArgumentNullException ex = new("serviceType");
			Diag.Dug(ex);
			throw ex;
		}
		/*
		else if (serviceType == typeof(IBDesignerExplorerServices))
		{
			object service = new DesignerExplorerServices()
				?? throw Diag.ExceptionService(serviceType);

			return service;
		}
		else if (serviceType == typeof(IBDesignerOnlineServices))
		{
			object service = new DesignerOnlineServices()
				?? throw Diag.ExceptionService(serviceType);

			return service;
		}
		*/
		else if (serviceType.IsInstanceOfType(this))
		{
			return this;
		}

		return await base.CreateServiceInstanceAsync(serviceType, token);
	}



	// ---------------------------------------------------------------------------------
	/// <summary>
	/// Asynchronous initialization of the package. The class must register services it
	/// requires using the ServicesCreatorCallback method.
	/// </summary>
	// ---------------------------------------------------------------------------------
	protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
	{
		Progress(progress, "Initializing Language services...");
		

		Progress(progress, "Registering Language service...");

		ServiceContainer.AddService(typeof(LsbLanguageService), ServicesCreatorCallbackAsync, promote: true);

		Progress(progress, "Registering Language service... Done.");

		

		Progress(progress, "Loading Language service preferences...");

		// AdvancedPreferencesModel advancedPreferences = await AdvancedPreferencesModel.GetLiveInstanceAsync();
		// await advancedPreferences.LoadAsync();

		Progress(progress, "Loading Language service preferences... Done.");

		

		await base.InitializeAsync(cancellationToken, progress);

		Progress(progress, "Initializing Language services... Done.");

	}


	// ---------------------------------------------------------------------------------
	/// <summary>
	/// Final asynchronous initialization tasks for the package that must occur after
	/// all descendents and ancestors have completed their InitializeAsync() tasks.
	/// It is the final descendent package class's responsibility to initiate the call
	/// to FinalizeAsync.
	/// </summary>
	// ---------------------------------------------------------------------------------
	public override async Task FinalizeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
	{
		Diag.ThrowIfNotOnUIThread();

		if (cancellationToken.IsCancellationRequested || ApcManager.IdeShutdownState)
			return;


		Progress(progress, "Finalizing Language services initialization...");

		await base.FinalizeAsync(cancellationToken, progress);



		Progress(progress, "Finalizing: Proffering Language Services...");

		// TODO: Used by Declarations.

		IComponentModel componentModel = await GetServiceAsync<SComponentModel, IComponentModel>();

		TextUndoHistoryRegistrySvc = componentModel.GetService<ITextUndoHistoryRegistry>();
		EditorAdaptersFactorySvc = componentModel.GetService<IVsEditorAdaptersFactoryService>();

		// _ = SqlSchemaModel.ModelSchema;

		IsInitialized = true;

		Progress(progress, "Finalizing: Proffering Language Services... Done.");

		

		Progress(progress, "Finalizing Language services initialization... Done.");

	}



	// ---------------------------------------------------------------------------------
	/// <summary>
	/// Initializes and configures a service of the specified type that is used by this
	/// Package.
	/// Configuration is performed by the class requiring the service.
	/// The actual instance creation of the service is the responsibility of the class
	/// Package that has access to the final descendent class of the Service.
	/// </summary>
	// ---------------------------------------------------------------------------------
	public override object ServicesCreatorCallback(IServiceContainer container, Type serviceType)
	{

		if (typeof(LsbLanguageService) == serviceType)
		{
			if (_LanguageService != null)
				return _LanguageService;

			_LanguageService = new LsbLanguageService(this);
			_LanguageService.SetSite(this);


			if (_ComponentID == 0 && GetService(typeof(SOleComponentManager)) is IOleComponentManager oleComponentManager)
			{
				OLECRINFO[] array = new OLECRINFO[1];
				array[0].cbSize = (uint)Marshal.SizeOf(typeof(OLECRINFO));
				array[0].grfcrf = 3u;
				array[0].grfcadvf = 7u;
				array[0].uIdleTimeInterval = 1000u;
				oleComponentManager.FRegisterComponent(this, array, out _ComponentID);
			}

			return _LanguageService;
		}


		return base.ServicesCreatorCallback(container, serviceType);
	}



	// ---------------------------------------------------------------------------------
	/// <summary>
	/// Initializes and configures a service of the specified type that is used by this
	/// Package.
	/// Configuration is performed by the class requiring the service.
	/// The actual instance creation of the service is the responsibility of the class
	/// Package that has access to the final descendent class of the Service.
	/// </summary>
	// ---------------------------------------------------------------------------------
	public override async Task<object> ServicesCreatorCallbackAsync(IAsyncServiceContainer container, CancellationToken token, Type serviceType)
	{


		if (serviceType == typeof(LsbLanguageService))
			return ServicesCreatorCallback(this, serviceType);


		return await base.ServicesCreatorCallbackAsync(container, token, serviceType);
	}


	#endregion Package Methods Implementations





	// =========================================================================================================
	#region Methods - LanguageExtensionPackage
	// =========================================================================================================


	public int FContinueMessageLoop(uint uReason, IntPtr pvLoopData, MSG[] pMsgPeeked)
	{
		return 1;
	}

	public int FDoIdle(uint grfidlef)
	{
		Instance.LanguageService?.OnIdle((grfidlef & 1) != 0);
		return 0;
	}

	public int FPreTranslateMessage(MSG[] pMsg)
	{
		return 0;
	}

	public int FQueryTerminate(int fPromptUser)
	{
		return 1;
	}

	public int FReserved1(uint dwReserved, uint message, IntPtr wParam, IntPtr lParam)
	{
		return 1;
	}



	public IntPtr HwndGetWindow(uint dwWhich, uint dwReserved)
	{
		return IntPtr.Zero;
	}


	public LsbLanguagePreferences GetUserPreferences()
	{
		if (_UserPreferences == null)
		{
			_UserPreferences = new LsbLanguagePreferences(this, typeof(LsbLanguageService).GUID, PackageData.LanguageLongName);
			_UserPreferences.Init();
		}
		return _UserPreferences;
	}




	public override void SaveUserPreferences()
	{
		using RegistryKey registryKey = base.UserRegistryRoot;

		string settingsKey = PackageData.RegistrySettingsKey;
		object languagePreferences = GetUserPreferences();
		RegistryKey registryKey2 = registryKey.OpenSubKey(settingsKey, writable: true);

		registryKey2 ??= registryKey.CreateSubKey(settingsKey);

		IList<string> savedProperties = new List<string>();

		using (registryKey2)
		{
			foreach (PropertyDescriptor property in TypeDescriptor.GetProperties(languagePreferences, new Attribute[0]))
			{
				TypeConverter converter = property.Converter;
				if (converter.CanConvertTo(typeof(string)) && converter.CanConvertFrom(typeof(string)))
				{
					savedProperties.Add(property.Name);
					registryKey2.SetValue(property.Name, converter.ConvertToInvariantString(property.GetValue(languagePreferences)));
				}
			}
		}

	}



	public void Terminate()
	{
	}



	// ---------------------------------------------------------------------------------
	/// <summary>
	/// Updates and saves LanguagePreferences from the settings model if exists.
	/// </summary>
	// ---------------------------------------------------------------------------------
	public void UpdateUserPreferences()
	{
		if (_UserPreferences != null /* && _UserPreferences.IsDirty */)
		{
			_UserPreferences.Update();
			_UserPreferences.Apply();
		}
	}



	private void UserDispose()
	{
		if (_UserPreferences != null)
		{
			_UserPreferences.Dispose();
			_UserPreferences = null;
		}
	}


	#endregion Methods and Implementations




	// =========================================================================================================
	#region Event handlers - LanguageExtensionPackage
	// =========================================================================================================


	public void OnActivationChange(IOleComponent pic, int fSameComponent, OLECRINFO[] pcrinfo, int fHostIsActivating, OLECHOSTINFO[] pchostinfo, uint dwReserved)
	{
	}

	public void OnAppActivate(int fActive, uint dwOtherThreadID)
	{
	}

	public void OnEnterState(uint uStateID, int fEnter)
	{
	}

	public void OnLoseActivation()
	{
	}


	#endregion Event handlers


}

#endregion Class Declaration
