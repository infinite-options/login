using System;
namespace BlankApp.LogInClasses
{
    public class Data
    {
        public int height { get; set; }
        public bool is_silhouette { get; set; }
        public string url { get; set; }
        public int width { get; set; }
    }

    public class Picture
    {
        public Data data { get; set; }
    }

    public class FacebookResponse
    {
        public string email { get; set; }
        public string name { get; set; }
        public Picture picture { get; set; }
        public string id { get; set; }
    }
}
