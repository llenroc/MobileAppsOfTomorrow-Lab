﻿using HappyXamDevs.Services;
using Plugin.Media;
using Plugin.Media.Abstractions;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;

namespace HappyXamDevs.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private IAzureService azureService;

        private bool isRefreshing;

        public bool IsRefreshing
        {
            get => isRefreshing;
            set => Set(ref isRefreshing, value);
        }

        public ObservableCollection<PhotoViewModel> Photos { get; } = new ObservableCollection<PhotoViewModel>();

        public ICommand RefreshCommand { get; }

        public ICommand SelectFromLibraryCommand { get; }

        public ICommand TakePhotoCommand { get; }

        public MainViewModel()
        {
            MessagingCenter.Subscribe<IAzureService>(this, "LoggedIn", async s => await Refresh());

            TakePhotoCommand = new Command(async () => await TakePhoto());
            SelectFromLibraryCommand = new Command(async () => await SelectFromLibrary());
            RefreshCommand = new Command(async () => await Refresh());
            azureService = DependencyService.Get<IAzureService>();
        }

        private async Task Refresh()
        {
            var photos = await azureService.GetAllPhotoMetadata();

            if (!Photos.Any())
            {
                foreach (var photo in photos.OrderByDescending(p => p.Timestamp))
                {
                    Photos.Add(new PhotoViewModel(photo));
                }
            }
            else
            {
                var latest = Photos[0].Timestamp;
                foreach (var photo in photos.Where(p => p.Timestamp > latest).OrderBy(p => p.Timestamp))
                {
                    Photos.Insert(0, new PhotoViewModel(photo));
                }
            }

            IsRefreshing = false;
        }

        private async Task SelectFromLibrary()
        {
            var options = new PickMediaOptions { PhotoSize = PhotoSize.Medium };
            var photo = await CrossMedia.Current.PickPhotoAsync(options);
            if (!await ValidatePhoto(photo)) return;
            await azureService.UploadPhoto(photo);
        }

        private async Task TakePhoto()
        {
            var options = new StoreCameraMediaOptions { PhotoSize = PhotoSize.Medium };
            var photo = await CrossMedia.Current.TakePhotoAsync(options);
            if (!await ValidatePhoto(photo)) return;
            await azureService.UploadPhoto(photo);
        }

        private async Task<bool> ValidatePhoto(MediaFile photo)
        {
            if (photo == null) return false;
            var isHappy = await azureService.VerifyHappyFace(photo);
            if (isHappy) return true;

            await Application.Current.MainPage.DisplayAlert("Sad panda",
                                                            "I can't find any happy Xamarin developers in this picture. Please try again.",
                                                            "Will do!");
            return false;
        }
    }
}