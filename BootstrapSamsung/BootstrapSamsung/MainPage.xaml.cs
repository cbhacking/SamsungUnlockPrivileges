using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using BootstrapSamsung.Resources;

using BootstrapSamsungComponent;

namespace BootstrapSamsung
{
	public partial class MainPage : PhoneApplicationPage
	{
		// Constructor
		public MainPage ()
		{
			InitializeComponent();

			// Sample code to localize the ApplicationBar
			//BuildLocalizedApplicationBar();
		}

		private void PhoneApplicationPage_Loaded (object sender, RoutedEventArgs e)
		{
			uint ret = 0;
			try
			{
				ret = NativeComponent.UnlockCapability();
				if (1 == ret)
				{
					MessageBox.Show("SUCCESS! You can now sideload the app to unlock all capabilities.\nThis app can now be exited and removed.");
				}
				else
				{
					MessageBox.Show("POSSIBLE FAILURE: the returned value was " + ret);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("EXCEPTION; please report this.\n" + ex.ToString());
			}
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