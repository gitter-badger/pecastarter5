﻿using System;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Codeplex.Data;
using Progressive.PecaStarter5.Plugins;

namespace Progressive.PecaStarter5.Models.YellowPages
{
    class WebApiYellowPages : YellowPagesBase, IExternalYellowPages
    {
        private string password = "";

        public override bool IsExternal { get { return true; } }
        public string BroadcastUrl { get; set; }
        public string UpdateUrl { get; set; }
        public string StopUrl { get; set; }
        public string[] BroadcastParameters { get; set; }
        public string[] UpdateParameters { get; set; }
        public string[] StopParameters { get; set; }

        public Task UpdateAsync(NameValueCollection parameters)
        {
            return Post(UpdateUrl, parameters);
        }

        public Task StopAsync(NameValueCollection parameters)
        {
            return Post(StopUrl, parameters);
        }

        private async Task Post(string url, NameValueCollection parameters)
        {
            string response;
            using (var client = new WebClient())
            {
                response = await Task.Factory.StartNew(() => Encoding.UTF8.GetString(client.UploadValues(url, parameters)));
            }
            dynamic json = DynamicJson.Parse(response);
            if (json.result != "successful")
            {
                throw new YellowPagesException(json.message);
            }
        }

        #region IPlugin メンバー

        public Task OnBroadcastedAsync(BroadcastingParameter parameter)
        {
            password = parameter.YellowPagesParameters["password"];
            var nvc = new NameValueCollection();
            foreach (var param in BroadcastParameters)
            {
                nvc.Add(param, GetParameterValue(param, parameter));
            }
            return Post(BroadcastUrl, nvc);
        }

        private string GetParameterValue(string parameterKey, BroadcastingParameter parameter)
        {
            switch (parameterKey)
            {
                case "name":
                    return parameter.BroadcastParameter.Name;
                case "bitrate":
                    return parameter.Bitrate.ToString();
                case "tags":
                    return parameter.BroadcastParameter.Genre;
                case "description":
                    return parameter.BroadcastParameter.Description;
                case "comment":
                    return parameter.BroadcastParameter.Comment;
                case "contact_url":
                    return parameter.BroadcastParameter.ContactUrl;
                case "protocol":
                    return "Peercast";
                case "stream_url":
                    return "http://localhost:7144/pls/" + parameter.Id;
                case "type":
                    return parameter.BroadcastParameter.Type;
                case "password":
                    return parameter.YellowPagesParameters.Single(x => x.Key == "password").Value;
                case "listeners_invisibility":
                    return parameter.YellowPagesParameters.Single(x => x.Key == "listeners_invisibility").Value;
                case "result_format":
                    return "json";
                default:
                    throw new ArgumentException();
            }
        }

        public Task OnUpdatedAsync(UpdatedParameter parameter)
        {
            password = parameter.YellowPagesParameters["password"];
            var nvc = new NameValueCollection();
            foreach (var param in UpdateParameters)
            {
                nvc.Add(param, GetParameterValue(param, parameter));
            }
            return Post(UpdateUrl, nvc);
        }

        private string GetParameterValue(string parameterKey, UpdatedParameter parameter)
        {
            switch (parameterKey)
            {
                case "name":
                    return parameter.UpdateParameter.Name;
                case "tags":
                    return parameter.UpdateParameter.Genre;
                case "description":
                    return parameter.UpdateParameter.Description;
                case "comment":
                    return parameter.UpdateParameter.Comment;
                case "contact_url":
                    return parameter.UpdateParameter.ContactUrl;
                case "password":
                    return parameter.YellowPagesParameters.Single(x => x.Key == "password").Value;
                case "listeners_invisibility":
                    return parameter.YellowPagesParameters.Single(x => x.Key == "listeners_invisibility").Value;
                case "result_format":
                    return "json";
                default:
                    throw new ArgumentException();
            }
        }

        public Task OnStopedAsync(StoppedParameter parameter)
        {
            var nvc = new NameValueCollection();
            foreach (var param in StopParameters)
            {
                nvc.Add(param, GetParameterValue(param, parameter));
            }
            return Post(StopUrl, nvc);
        }

        private string GetParameterValue(string parameterKey, StoppedParameter parameter)
        {
            switch (parameterKey)
            {
                case "name":
                    return parameter.Name;
                case "password":
                    return parameter.YellowPagesParameters.Single(x => x.Key == "password").Value;
                case "result_format":
                    return "json";
                default:
                    throw new ArgumentException();
            }
        }

        public Task OnTickedAsync(string name, int relays, int listeners)
        {
            if (string.IsNullOrEmpty(password))
                return Task.Factory.StartNew(() => { });

            var nvc = new NameValueCollection();
            nvc.Add("name", name);
            nvc.Add("password", password);
            nvc.Add("listeners", Math.Min(relays, listeners).ToString());
            return Post(UpdateUrl, nvc);
        }

        #endregion
    }

    public class YellowPagesException : Exception
    {
        public YellowPagesException(string message)
            :base(message)
        {
        }
    }
}
