using System;
using System.Collections.Generic;

using Xamarin.Forms;

namespace BlankApp
{
    public partial class HomePage : ContentPage
    {
        public HomePage()
        {
            InitializeComponent();
        }

        void Button_Clicked(System.Object sender, System.EventArgs e)
        {
            Application.Current.MainPage = new LogIn();
        }
    }
}
