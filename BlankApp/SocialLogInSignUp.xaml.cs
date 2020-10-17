using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Newtonsoft.Json;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Maps;
using BlankApp.LogInClasses;
using BlankApp.Constants;

namespace BlankApp
{
    public partial class SocialLogInSignUp : ContentPage
    {
        public SignUpPost socialSignUp = new SignUpPost();
        public bool isAddressValidated = false;

        public SocialLogInSignUp(string firstName, string lastName, string emailAddress, string accessToken, string refreshToken, string platform)
        {
            InitializeComponent();
            InitializeSignUpPost();
            userFirstName.Text = firstName;
            userLastName.Text = lastName;
            userEmailAddress.Text = emailAddress;

            socialSignUp.email = emailAddress;
            socialSignUp.first_name = firstName;
            socialSignUp.last_name = lastName;
            socialSignUp.access_token = accessToken;
            socialSignUp.refresh_token = refreshToken;
            socialSignUp.social = platform;
        }

        void InitializeSignUpPost()
        {
            socialSignUp.email = "";
            socialSignUp.first_name = "";
            socialSignUp.last_name = "";
            socialSignUp.phone_number = "";
            socialSignUp.address = "";
            socialSignUp.unit = "";
            socialSignUp.city = "";
            socialSignUp.state = "";
            socialSignUp.zip_code = "";
            socialSignUp.latitude = "0.0";
            socialSignUp.longitude = "0.0";
            socialSignUp.referral_source = "MOBILE";
            socialSignUp.role = "CUSTOMER";
            socialSignUp.access_token = "";
            socialSignUp.refresh_token = "";
            socialSignUp.social = "";
            socialSignUp.password = "";
        }

        async void ValidatingAddressClick(System.Object sender, System.EventArgs e)
        {
            
            if (userPhoneNumber.Text != null && userPhoneNumber.Text.Length == 10)
            {
                socialSignUp.phone_number = userPhoneNumber.Text.Trim();
            }
            else
            {
                await DisplayAlert("Alert!", "Please enter a 10 valid phone number", "OK");
            }

            if(userAddress.Text != null)
            {
                socialSignUp.address = userAddress.Text.Trim();
            }
            else
            {
                await DisplayAlert("Alert!", "Please enter your address", "OK");
            }

            if(userUnitNumber.Text != null)
            {
                socialSignUp.unit = userUnitNumber.Text.Trim();
            }

            if (userLastName != null)
            {
                socialSignUp.last_name = userLastName.Text.Trim();
            }

            if (userCity.Text != null)
            {
                socialSignUp.city = userCity.Text.Trim();
            }
            else
            {
                await DisplayAlert("Alert!", "Please enter your city", "OK");
            }

            if (userState.Text != null)
            {
                socialSignUp.state = userState.Text.Trim();
            }
            else
            {
                await DisplayAlert("Alert!", "Please enter your state", "OK");
            }

            if(userZipcode.Text != null && userZipcode.Text.Length == 5)
            {
                socialSignUp.zip_code = userZipcode.Text.Trim();
            }
            else
            {
                await DisplayAlert("Alert!", "Please enter a 5 digit zipcode", "OK");
            }

            // Setting request for USPS API
            XDocument requestDoc = new XDocument(
                new XElement("AddressValidateRequest",
                new XAttribute("USERID", "400INFIN1745"),
                new XElement("Revision", "1"),
                new XElement("Address",
                new XAttribute("ID", "0"),
                new XElement("Address1", userAddress.Text),
                new XElement("Address2", userUnitNumber.Text),
                new XElement("City", userCity.Text),
                new XElement("State", userState.Text),
                new XElement("Zip5", userZipcode.Text),
                new XElement("Zip4", "")
                     )
                 )
             );
            var url = "http://production.shippingapis.com/ShippingAPI.dll?API=Verify&XML=" + requestDoc;
            Console.WriteLine(url);
            var client = new WebClient();
            var response = client.DownloadString(url);

            var xdoc = XDocument.Parse(response.ToString());
            Console.WriteLine(xdoc);
            string latitude = "0";
            string longitude = "0";


            foreach (XElement element in xdoc.Descendants("Address"))
            {

                System.Diagnostics.Debug.WriteLine(GetXMLElement(element, "Error"));
                if (GetXMLElement(element, "Error").Equals(""))
                {
                    if (GetXMLElement(element, "DPVConfirmation").Equals("Y") && GetXMLElement(element, "Zip5").Equals(userZipcode.Text) && GetXMLElement(element, "City").Equals(userCity.Text.ToUpper().Trim())) // Best case
                    {
                        Geocoder geoCoder = new Geocoder();

                        IEnumerable<Position> approximateLocations = await geoCoder.GetPositionsForAddressAsync(userAddress.Text + "," + userCity.Text + "," + userState.Text);
                        Position position = approximateLocations.FirstOrDefault();

                        latitude = $"{position.Latitude}";
                        longitude = $"{position.Longitude}";

                        socialSignUp.latitude = latitude;
                        socialSignUp.longitude = longitude;

                        isAddressValidated = true;

                        //map.MapType = MapType.Street;
                        //var mapSpan = new MapSpan(position, 0.001, 0.001);

                        //Pin address = new Pin();
                        //address.Label = "Delivery Address";
                        //address.Type = PinType.SearchResult;
                        //address.Position = position;

                        //map.MoveToRegion(mapSpan);
                        //map.Pins.Add(address);
                        break;
                    }
                    else if (GetXMLElement(element, "DPVConfirmation").Equals("D"))
                    {
                        // await DisplayAlert("Alert!", "Address is missing information like 'Apartment number'.", "Ok");
                        // return;
                    }
                    else
                    {
                        // await DisplayAlert("Alert!", "Seems like your address is invalid.", "Ok");
                        // return;
                    }
                }
                else
                {   // USPS sents an error saying address not found in there records. In other words, this address is not valid because it does not exits.
                    // Console.WriteLine("Seems like your address is invalid.");
                    // await DisplayAlert("Alert!", "Error from USPS. The address you entered was not found.", "Ok");
                    // return;
                }
            }

            if (!isAddressValidated)
            {
                await DisplayAlert("Message", "We were not able to validate your address. Please try again.", "OK");
            }
        }

        async void SignUpSocialUserClick(System.Object sender, System.EventArgs e)
        {
            if (isAddressValidated)
            {
                DateTime today = DateTime.Now;
                DateTime expirationDate = today.AddDays(Constant.days);

                Application.Current.Properties["time_stamp"] = expirationDate;
                socialSignUp.social_timestamp = expirationDate.ToString("yyyy-MM-dd HH:mm:ss");

                var signUpSerializedObject = JsonConvert.SerializeObject(socialSignUp);
                var singUpContent = new StringContent(signUpSerializedObject, Encoding.UTF8, "application/json");

                System.Diagnostics.Debug.WriteLine(signUpSerializedObject);

                var client = new HttpClient();
                var RDSResponse = await client.PostAsync(Constant.SignUpUrl, singUpContent);
                var RDSMessage = await RDSResponse.Content.ReadAsStringAsync();

                System.Diagnostics.Debug.WriteLine(RDSMessage);

                if (RDSResponse.IsSuccessStatusCode)
                {
                    var RDSData = JsonConvert.DeserializeObject<SignUpResponse>(RDSMessage);

                    System.Diagnostics.Debug.WriteLine("This are the variable you can use from RDSMessage");
                    System.Diagnostics.Debug.WriteLine("First Name: "+ RDSData.result.first_name);
                    System.Diagnostics.Debug.WriteLine("Last Name: " + RDSData.result.last_name);
                    System.Diagnostics.Debug.WriteLine("Customer ID: " + RDSData.result.customer_uid);
                    System.Diagnostics.Debug.WriteLine("Access Token: " + RDSData.result.access_token);
                    System.Diagnostics.Debug.WriteLine("Refresh Token: " + RDSData.result.refresh_token);

                    Application.Current.Properties["uid"] = RDSData.result.customer_uid;
                    Application.Current.Properties["platform"] = socialSignUp.social;
                    Application.Current.MainPage = new HomePage();
                }
            }
            else
            {
                await DisplayAlert("Message", "We weren't able to sign you up", "OK");
            }
        }

        public static string GetXMLElement(XElement element, string name)
        {
            var el = element.Element(name);
            if (el != null)
            {
                return el.Value;
            }
            return "";
        }

        public static string GetXMLAttribute(XElement element, string name)
        {
            var el = element.Attribute(name);
            if (el != null)
            {
                return el.Value;
            }
            return "";
        }
    }
}
