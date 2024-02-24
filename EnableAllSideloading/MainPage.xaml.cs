using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using EnableAllSideloading.Resources;

using Registry;

namespace EnableAllSideloading
{
	public partial class MainPage : PhoneApplicationPage
	{

		const String PATH = @"SOFTWARE\Microsoft\SecurityManager\CapabilityClasses";

		// Constructor
		public MainPage ()
		{
			InitializeComponent();

			// Sample code to localize the ApplicationBar
			//BuildLocalizedApplicationBar();
		}

		private void PhoneApplicationPage_Loaded (object sender, RoutedEventArgs e)
		{
			try
			{
				uint init = NativeRegistry.InitializeRoot();
				if (init != 0)
				{
					MessageBox.Show("RootRPC initialization failed with status " + init + "!");
					return;
				}
				if (InteropUnlock() && UnlockCapabilities() && UnlockAuthRules())
				{
					MessageBox.Show("You can now close and remove this app.");
					//			UnlockPrivateCaps();
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("Exception while enabling all capabilities!\n" + ex.ToString());
			}
		}

		static void UnlockPrivateCaps ()
		{
			byte[] data;
			if (!NativeRegistry.ReadBinary(
				RegistryHive.HKLM,
				@"SOFTWARE\Microsoft\SecurityManager\Capabilities\ID_CAP_PRIV_ACCOUNTPROVSVC",
				"ServiceCapabilitySID",
				out data))
			{
				MessageBox.Show("Error getting the service SID!\nThe Win32 error code is " + NativeRegistry.GetError());
				return;
			}
			if (!NativeRegistry.WriteBinary(
				RegistryHive.HKLM,
				@"SOFTWARE\Microsoft\SecurityManager\Capabilities\ID_CAP_PRIV_ACCOUNTPROVSVC",
				"ApplicationCapabilitySID",
				data))
			{
				MessageBox.Show("Error setting the application SID!\nThe Win32 error code is " + NativeRegistry.GetError());
				return;
			}
			if (!NativeRegistry.WriteDWORD(
				RegistryHive.HKLM,
				@"SOFTWARE\Microsoft\SecurityManager\Capabilities\ID_CAP_PRIV_ACCOUNTPROVSVC",
				"CapabilityType",
				1))
			{
				MessageBox.Show("Error setting the capability type!\nThe Win32 error code is " + NativeRegistry.GetError());
				return;
			}
			MessageBox.Show("It could now be possible to sideload ID_CAP_PRIV_ACCOUNTPROVSVC apps");
		}

		static bool InteropUnlock ()
		{
			const String DEVICEREG = @"SOFTWARE\Microsoft\DeviceReg";
			if (!NativeRegistry.WriteString(
				RegistryHive.HKLM,
				DEVICEREG,
				"PortalUrlProd",
				"https://127.0.0.1/"))
			{
				MessageBox.Show("Error disabling PortalUrlProd!\nThe Win32 error code is " + NativeRegistry.GetError());
				return false;
			}
			if (!NativeRegistry.WriteString(
				RegistryHive.HKLM,
				DEVICEREG,
				"PortalUrlInt",
				"https://127.0.0.1/"))
			{
				MessageBox.Show("Error disabling PortalUrlInt!\nThe Win32 error code is " + NativeRegistry.GetError());
				return false;
			}
            if (!NativeRegistry.WriteDWORD(
				RegistryHive.HKLM,
				DEVICEREG,
				"Period",
				0xFFFFFFFF))
			{
				MessageBox.Show("Error setting maximum DeviceReg period!\nThe Win32 error code is " + NativeRegistry.GetError());
				return false;
			}
            if (!NativeRegistry.WriteDWORD(
				RegistryHive.HKLM,
				DEVICEREG,
				"FuzzingPercentage",
				0x0))
			{
				MessageBox.Show("Error setting FuzzingPercentage to 0!\nThe Win32 error code is " + NativeRegistry.GetError());
				return false;
			}
            if (!NativeRegistry.WriteDWORD(
				RegistryHive.HKLM,
				DEVICEREG + "\\INSTALL",
				"MaxUnsignedApp",
				0x9999))
			{
				MessageBox.Show("Error interop-unlocking!\nThe Win32 error code is " + NativeRegistry.GetError());
				return false;
			}
			MessageBox.Show("Success 1 of 3: Device is interop-unlocked and has re-lock checks disabled!");
			return true;
		}

		static bool UnlockCapabilities ()
		{
			String[] capabilities;
			String[] classes;
			String[] thirdparty = new String[] { "CAPABILITY_CLASS_THIRD_PARTY_APPLICATIONS" };
			if (!NativeRegistry.GetSubKeyNames(RegistryHive.HKLM, @"SOFTWARE\Microsoft\SecurityManager\Capabilities", out capabilities))
			{
				MessageBox.Show("Error getting list of all capabilities!\nThe Win32 error code is " + NativeRegistry.GetError());
				return false;
			}
			foreach (String cap in capabilities)
			{
				// First, make sure it's a capability...
				if ((!cap.StartsWith("ID_CAP_")) || cap.StartsWith("ID_CAP_PRIV_"))
					continue;
				// Modify the class list semi-intelligently
				if (!NativeRegistry.ReadMultiString(RegistryHive.HKLM, PATH, cap, out classes))
				{
					uint err = NativeRegistry.GetError();
					switch (err)
					{
					case 2:
						// Value does not exist yet; just write it
						if (!NativeRegistry.WriteMultiString(RegistryHive.HKLM, PATH, cap, thirdparty))
						{
							MessageBox.Show("Error unlocking capability \"" + cap + "\"!\nThe Win32 error code is " + NativeRegistry.GetError());
							return false;
						}
						// Go to next item...
						continue;
					case 1630:
						// Not a MultiString
						classes = new String[1];
						if (!NativeRegistry.ReadString(RegistryHive.HKLM, PATH, cap, out classes[0]))
						{
							// It *should* have been a string...
							MessageBox.Show("Error reading current category classes for capability \"" + cap + "\"\nThe error code is " + NativeRegistry.GetError());
							return false;
						}
						if (!fixCaps(classes, cap))
							return false;
						break;
					default:
						// Some other problem...
						MessageBox.Show("Error reading current category classes for capability \"" + cap + "\"\nThe error code is " + NativeRegistry.GetError());
						return false;
					}
				}
				if (!fixCaps(classes, cap))
					return false;
			}
			MessageBox.Show("Success 2 of 3: All capabilities are now in the third-party capability class!");
			return true;
		}

		static bool fixCaps (String[] arr, String cap)
		{
			LinkedList<String> working = new LinkedList<string>(arr);
			while (working.Remove("CAPABILITY_CLASS_SECOND_PARTY_APPLICATIONS"))
				;
			while (working.Remove("CAPABILITY_CLASS_FIRST_PARTY_APPLICATIONS"))
				;
			if (!working.Contains("CAPABILITY_CLASS_THIRD_PARTY_APPLICATIONS"))
				working.AddFirst("CAPABILITY_CLASS_THIRD_PARTY_APPLICATIONS");
			arr = working.ToArray();
			if (!NativeRegistry.WriteMultiString(RegistryHive.HKLM, PATH, cap, arr))
			{
				MessageBox.Show("Error unlocking capability \"" + cap + "\"!\nThe Win32 error code is " + NativeRegistry.GetError());
				return false;
			}
			return true;
		}

		static bool UnlockAuthRules ()
		{
			// Treat ISV unlock status like it's OEM unlock status
			// Original value was "PRINCIPAL_CLASS_ISV_DEVELOPER_UNLOCK"
			if (!NativeRegistry.WriteString(
				RegistryHive.HKLM,
				@"SOFTWARE\Microsoft\SecurityManager\AuthorizationRules\Capability\CAPABILITY_RULE_ISV_DEVELOPER_UNLOCK",
				"PrincipalClass",
				"PRINCIPAL_CLASS_OEM_DEVELOPER_UNLOCK"))
			{
				MessageBox.Show("Error setting dev unlock principal class!\nThe Win32 error code is " + NativeRegistry.GetError());
				return false;
			}
				// Allow ISV unlock status to sideload apps with first-party capabilities
				// Original value was "CAPABILITY_CLASS_THIRD_PARTY_APPLICATIONS"
			if (!NativeRegistry.WriteString(
				RegistryHive.HKLM,
				@"SOFTWARE\Microsoft\SecurityManager\AuthorizationRules\Capability\CAPABILITY_RULE_ISV_DEVELOPER_UNLOCK",
				"CapabilityClass",
				"CAPABILITY_CLASS_FIRST_PARTY_APPLICATIONS"))
			{
				MessageBox.Show("Error setting dev unlock capability class!\nThe Win32 error code is " + NativeRegistry.GetError());
				return false;
			}
			MessageBox.Show("Success 3 of 3: The phone treats all dev-unlock as interop-unlock!");
			return true;
		}

		// Sample code for building a localized ApplicationBar
		//private void BuildLocalizedApplicationBar()
		//{
		//    // Set the page's ApplicationBar to a new instance of ApplicationBar.
		//    ApplicationBar = new ApplicationBar();

		//    // Create a new button and set the text value to the localized string from AppResources.
		//    ApplicationBarIconButton appBarButton = new ApplicationBarIconButton(new Uri("/Assets/AppBar/appbar.add.rest.png", UriKind.Relative));
		//    appBarButton.Text = AppResources.AppBarButtonText;
		//    ApplicationBar.Buttons.Add(appBarButton);

		//    // Create a new menu item with the localized string from AppResources.
		//    ApplicationBarMenuItem appBarMenuItem = new ApplicationBarMenuItem(AppResources.AppBarMenuItemText);
		//    ApplicationBar.MenuItems.Add(appBarMenuItem);
		//}
	}
}