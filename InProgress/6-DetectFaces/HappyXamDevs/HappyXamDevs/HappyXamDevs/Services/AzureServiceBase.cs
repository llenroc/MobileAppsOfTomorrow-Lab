﻿using Microsoft.Azure.CognitiveServices.Vision.Face;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;
using Microsoft.WindowsAzure.MobileServices;
using Plugin.Media.Abstractions;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace HappyXamDevs.Services
{
    public abstract class AzureServiceBase : IAzureService
    {
        private const string AuthTokenKey = "auth-token";
        private const string UserIdKey = "user-id";
        private FaceAPI faceApi;
        
#error REPLACE [YOUR AZURE APP NAME HERE]
        protected const string AzureAppName = "[YOUR AZURE APP NAME HERE]";
        protected readonly static string FunctionAppUrl = $"https://{AzureAppName}.azurewebsites.net";

        public MobileServiceClient Client { get; }

        protected AzureServiceBase()
        {
            Client = new MobileServiceClient(FunctionAppUrl);

#error REPLACE [YOUR API KEY HERE]
            var creds = new ApiKeyServiceClientCredentials("[YOUR API KEY HERE]");
            faceApi = new FaceAPI(creds)
            {
                AzureRegion = AzureRegions.Westeurope
            };
        }

        private void TryLoadUserDetails()
        {
            if (Client.CurrentUser != null) return;

            if (Application.Current.Properties.TryGetValue(AuthTokenKey, out var authToken) &&
                Application.Current.Properties.TryGetValue(UserIdKey, out var userId))
            {
                Client.CurrentUser = new MobileServiceUser(userId.ToString())
                {
                    MobileServiceAuthenticationToken = authToken.ToString()
                };
            }
        }

        protected abstract Task AuthenticateUser();

        public async Task<bool> Authenticate()
        {
            if (IsLoggedIn()) return true;
            await AuthenticateUser();

            if (Client.CurrentUser != null)
            {
                Application.Current.Properties[AuthTokenKey] = Client.CurrentUser.MobileServiceAuthenticationToken;
                Application.Current.Properties[UserIdKey] = Client.CurrentUser.UserId;
                await Application.Current.SavePropertiesAsync();
            }

            return IsLoggedIn();
        }

        public bool IsLoggedIn()
        {
            TryLoadUserDetails();
            return Client.CurrentUser != null;
        }

        public async Task<bool> VerifyHappyFace(MediaFile photo)
        {
            using (var s = photo.GetStream())
            {
                var faceAttributes = new List<FaceAttributeType> { FaceAttributeType.Emotion };
                var faces = await faceApi.Face.DetectWithStreamAsync(s, returnFaceAttributes: faceAttributes);
                return faces.Any() && faces.All(f => f.FaceAttributes.Emotion.Happiness > 0.75);
            }
        }
    }
}