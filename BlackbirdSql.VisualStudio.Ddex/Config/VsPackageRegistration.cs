﻿// $License = https://github.com/BlackbirdSQL/NETProvider-DDEX/blob/master/Docs/license.txt
// $Authors = GA Christos (greg@blackbirdsql.org)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

using BlackbirdSql.Core;
using BlackbirdSql.Core.Interfaces;
using BlackbirdSql.VisualStudio.Ddex.Interfaces;

using FirebirdSql.Data.FirebirdClient;

using Microsoft.VisualStudio.Shell;




namespace BlackbirdSql.VisualStudio.Ddex.Config;


internal sealed class VsPackageRegistration: RegistrationAttribute
{
	/// <summary>
	/// Registers the package in the local user's VS private registry
	/// </summary>
	/// <remarks>
	/// This is pretty much solid so commenting out diags
	/// </remarks>
	public override void Register(RegistrationContext context)
	{
		if (context == null)
		{
			ArgumentNullException ex = new("context");
			Diag.Dug(ex);
			throw ex;
		}

		Key key = null;
		Key key2 = null;
		Key key3;
		PackageSupportedObjects.RegistryValue registryValue;

		try
		{
			Type providerFactoryClass = typeof(FirebirdClientFactory);
			string invariantName = providerFactoryClass.Assembly.GetName().Name;
			// string invariantFullName = providerFactoryClass.Assembly.FullName;

			Type providerObjectFactoryInterface = typeof(IBProviderObjectFactory);
			string providerObjectFactoryAssembly = providerObjectFactoryInterface.Assembly.FullName;

			string dataSourceGuid = SystemData.DataSourceGuid.StartsWith("{")
				? SystemData.DataSourceGuid
				: $"{{{SystemData.DataSourceGuid}}}";

			string providerGuid = PackageData.ProviderGuid.StartsWith("{")
				? PackageData.ProviderGuid
				: $"{{{PackageData.ProviderGuid}}}";

			// Clean up
			context.RemoveKey("DataSources\\" + dataSourceGuid + "\\SupportingProviders\\" + providerGuid);

			// Add the Firebird data source (if not exists???)
			key = context.CreateKey("DataSources\\" + dataSourceGuid);

			key.SetValue(null, SystemData.DataProviderName);
			key.SetValue("DefaultProvider", providerGuid);


			// Add this package as a provider for the Firebird data source
			key2 = key.CreateSubkey("SupportingProviders");
			key3 = key2.CreateSubkey(providerGuid);
			key3.SetValue("DisplayName", SystemData.DataProviderName);
			key3.SetValue("UsingDescription", "DdexProvider_Description, "
				+ "BlackbirdSql.VisualStudio.Ddex.Properties.Resources, " + providerObjectFactoryAssembly);
			DisposeKey(ref key3);
			DisposeKey(ref key2);
			DisposeKey(ref key);

			key = context.CreateKey("DataProviders\\" + providerGuid);
			key.SetValue(null, Properties.Resources.Provider_DisplayName);
			key.SetValue("Assembly", providerObjectFactoryAssembly);

			key.SetValue("PlatformVersion", "2.0");
			key.SetValue("Technology", "{" + SystemData.TechnologyGuid + "}");
			key.SetValue("AssociatedSource", dataSourceGuid);
			key.SetValue("InvariantName", invariantName);
			key.SetValue("Description", "DdexProvider_Description, "
				+ "BlackbirdSql.VisualStudio.Ddex.Properties.Resources, " + providerObjectFactoryAssembly);
			key.SetValue("DisplayName", "DdexProvider_DisplayName, "
				+ "BlackbirdSql.VisualStudio.Ddex.Properties.Resources, " + providerObjectFactoryAssembly);
			key.SetValue("ShortDisplayName",
				"DdexProvider_ShortDisplayName, BlackbirdSql.VisualStudio.Ddex.Properties.Resources, "
				+ providerObjectFactoryAssembly);

			// With everything working correctly we should need no codebase, no gac registration, and no dotnet system/machine config
			// Just run the vsix and go.
			// key.SetValue("CodeBase", PackageData.CodeBase);

			Attribute customAttribute = Attribute.GetCustomAttribute(providerObjectFactoryInterface, typeof(GuidAttribute));
			if (customAttribute == null)
			{
				Debug.Assert(condition: false);
				ApplicationException ex = new("IBProviderObjectFactory doesn't have Guid attribute.");
				Diag.Dug(ex);
				throw ex;
			}


			if (customAttribute is not GuidAttribute guidAttribute)
			{
				Debug.Assert(condition: false);
				ApplicationException ex = new("IBProviderObjectFactory's Guid attribute has the incorrect type.");
				Diag.Dug(ex);
				throw ex;
			}

			key.SetValue("FactoryService", "{" + guidAttribute.Value + "}");

			// Add in the supported sevices
			key2 = key.CreateSubkey("SupportedObjects");
			
			foreach (KeyValuePair<string, int> implementation in PackageSupportedObjects.Implementations)
			{
				key3 = key2.CreateSubkey(implementation.Key);

				for (int i = 0; i < implementation.Value; i++)
				{
					registryValue = PackageSupportedObjects.Values[implementation.Key + ":" + i.ToString()];

					// Diag.Trace(implementation.Key + ": " + (registryValue.Name == null ? "null" : registryValue.Name) + ":" + registryValue.Value);
					key3.SetValue(registryValue.Name, registryValue.Value);
				}

				DisposeKey(ref key3);
			}

			// InstallGlobalAssemblyCache();


		}
		finally
		{
			DisposeKey(ref key2);
			DisposeKey(ref key);
		}
	}


	/// <summary>
	/// Deregisters the package in the local user's VS private registry
	/// </summary>
	public override void Unregister(RegistrationContext context)
	{
		if (context == null)
		{
			ArgumentNullException ex = new("context");
			Diag.Dug(ex);
			throw ex;
		}

		string dataSourceGuid = SystemData.DataSourceGuid.StartsWith("{")
			? SystemData.DataSourceGuid
			: $"{{{SystemData.DataSourceGuid}}}";

		string providerGuid = PackageData.ProviderGuid.StartsWith("{")
			? PackageData.ProviderGuid
			: $"{{{PackageData.ProviderGuid}}}";

		Type providerObjectFactoryInterface = typeof(IBProviderObjectFactory);
		Attribute customAttribute = Attribute.GetCustomAttribute(providerObjectFactoryInterface, typeof(GuidAttribute));

		if (customAttribute == null)
		{
			Debug.Assert(condition: false);
			ApplicationException ex = new("[BUG CHECK] IBProviderObjectFactory doesn't have Guid attribute.");
			Diag.Dug(ex);
			throw ex;
		}

		if (customAttribute is not GuidAttribute guidAttribute)
		{
			Debug.Assert(condition: false);
			ApplicationException ex = new("[BUG CHECK] IBProviderObjectFactory's Guid attribute has the incorrect type.");
			Diag.Dug(ex);
			throw ex;
		}
		Debug.Assert(guidAttribute.Value == SystemData.ObjectFactoryServiceGuid);

		context.RemoveValue("DataSources\\" + dataSourceGuid, "DefaultProvider");
		context.RemoveKey("DataSources\\" + dataSourceGuid + "\\SupportingProviders\\" + providerGuid);

		context.RemoveKey("DataProviders\\" + providerGuid);

		context.RemoveKey("Services\\{" + guidAttribute.Value + "}");

		// UninstallGlobalAssemblyCache();

	}




	private static void DisposeKey(ref Key key)
	{
		if (key != null)
		{
			Key lockKey = Interlocked.Exchange(ref key, null);
			lockKey.Close();
			((IDisposable)lockKey).Dispose();
		}
	}


	// We're not going to use the gac so this is all redundant. Can be deleted
	/*
	public static void InstallGlobalAssemblyCache()
	{
		Type providerFactoryClass = typeof(FirebirdClientFactory);


		// Diag.Trace("GAC Install: " + providerFactoryClass.Assembly.Location);

		Publish publisher = new Publish();
		publisher.GacInstall(factoryClass.Assembly.Location);
	}

	public static void UninstallGlobalAssemblyCache()
	{
		Type providerFactoryClass = typeof(FirebirdClientFactory);

		// Diag.Trace("GAC Uninstall: " + providerFactoryClass.Assembly.Location);
		Publish publisher = new Publish();

		try
		{
			publisher.GacRemove(factoryClass.Assembly.Location);
		}
		catch
		{
		}
	}
	*/
}