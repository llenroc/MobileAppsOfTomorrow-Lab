# Showing the photos on a mobile app

Now that your mobile app can download the photos, you need to display them on your app. A normal way to display such data is through a scrolling list, with the most recent items at the top. Xamarin.Forms provides a [`ListView`](https://docs.microsoft.com/xamarin/xamarin-forms/user-interface/listview/?WT.mc_id=mobileappsoftomorrow-workshop-jabenn) that you can use to display such a scrolling list.

List views can be bound to a collection of objects, and each item in the collection is used to supply the data for an item in the list. The usual way to expose such a list is using an [`ObservableCollection`](https://docs.microsoft.com/dotnet/api/system.collections.objectmodel.observablecollection-1?view=netstandard-2.0&WT.mc_id=mobileappsoftomorrow-workshop-jabenn). This collection type has events that get raised when the collection changes, such as having new items added. A list view bound to an observable collection will listen for these events, and if one is detected, the list will update to match. You can put a list view on the main page, and bind it to an observable collection on your main view model.

## Loading the photos in the view model

### Creating a view model for each photo

What object should be in the observable collection? Ideally you want a view model that represents each individual item, exposing the photo and metadata as properties.

1. Add a new class to the `ViewModels` folder called `PhotoViewModel`, deriving from `BaseViewModel`.

    ```cs
    public class PhotoViewModel : BaseViewModel
    {
    }
    ```

2. Add some read-only properties for the image, the caption and the tags. You will need to add a using directive for the `Xamarin.Forms` namespace.

    | Property    | Type          | Description                                                                                        |
    | ----------- | ------------- | -------------------------------------------------------------------------------------------------- |
    | `Caption`   | `string`      | The caption for the photo generated by the Computer Vision service                                 |
    | `Tags`      | `string`      | The tags for the photo generated by the Computer Vision service, concatenated into a single string |
    | `Timestamp` | `long`        | A timestamp for when the document was created                                                      |
    | `Photo`     | `ImageSource` | An image source for the photo, this will be created from the file name                             |

    ```cs
    public class PhotoViewModel : BaseViewModel
    {
        public string Caption { get; }
        public string Tags { get; }
        public long Timestamp { get; }
        public ImageSource Photo { get; }
    }
    ```

3. Add a constructor to this view model that takes a `PhotoMetadata` parameter. You will need to add a using directive for the `HappyXamDevs.Services` namespace.

    ```cs
    public PhotoViewModel(PhotoMetadata photoMetadata)
    {
    }
    ```

4. In this constructor, initialize the `Caption` and `Timestamp` properties using the values from the metadata.

    ```cs
    Caption = photoMetadata.Caption;
    Timestamp = photoMetadata.Timestamp;
    ```

5. Still in the constructor, concatenate the tags from the photo metadata together using the static `string.Join` method with a space as a delimiter, and set this on the `Tags` property. To be in keeping with tags in modern social media apps, add a "#" to each tag. You will need to add a using directive for the `System.Linq` namespace.

    ```cs
    Tags = string.Join(" ", photoMetadata.Tags.Select(t => $"#{t}"));
    ```

6. Create a new file image source for the file name from the photo metadata and assign that to the `Photo` property.

    ```cs
    Photo = ImageSource.FromFile(photoMetadata.FileName);
    ```

The full code for this class is below.

```cs
using HappyXamDevs.Services;
using System.Linq;
using Xamarin.Forms;

namespace HappyXamDevs.ViewModels
{
    public class PhotoViewModel : BaseViewModel
    {
        public PhotoViewModel(PhotoMetadata photoMetadata)
        {
            Caption = photoMetadata.Caption;
            Timestamp = photoMetadata.Timestamp;
            Tags = string.Join(" ", photoMetadata.Tags.Select(t => $"#{t}"));
            Photo = ImageSource.FromFile(photoMetadata.FileName);
        }

        public string Caption { get; }
        public string Tags { get; }
        public long Timestamp { get; }
        public ImageSource Photo { get; }
    }
}
```

### Adding photos to the main view model

The main view model needs to expose an observable collection of photo view models that can be bound to the UI. It also needs a way to load the photos, and that can be implemented in a command that can be executed when the view loads.

1. Open `MainViewModel` and add a read-only public property called `Photos`, initializing this inline. You will need to add a using directive for the `System.Collections.ObjectModel` namespace.

    ```cs
    public ObservableCollection<PhotoViewModel> Photos { get; } = new ObservableCollection<PhotoViewModel>();
    ```

2. Add a new command called `RefreshCommand` to refresh the photos from the Azure service.

    ```cs
    public ICommand RefreshCommand { get; }
    ```

3. In the constructor, initialize this command using a new method called `Refresh`.

    ```cs
    public MainViewModel()
    {
        ...
        RefreshCommand = new Command(async () => await Refresh());
    }
    ```

4. Add this `Refresh` method as an `async` method.

    ```cs
    async Task Refresh()
    {
    }
    ```

5. In the `Refresh` method, start by loading all the photo metadata from the Azure service. Remember that this call will also download any photos that have not already been downloaded.

    ```cs
     var photos = await azureService.GetAllPhotoMetadata();
    ```

6. The `Refresh` method will be used initially when the page is loaded, so the `Photos` collection will be empty. Later, you will use this to refresh the UI manually so this method needs to only add items to the collection if they are not already present. To start with, check if the collection is empty, and if so add new `PhotoViewModel` items created using the photo metadata and ordered descending by their timestamp (so that the newest item with the largest time stamp is first in the collection). You will need to add a using directive for the `System.Linq` namespace.

    ```cs
    if (!Photos.Any())
    {
        foreach (var photo in photos.OrderByDescending(p => p.Timestamp))
        {
            Photos.Add(new PhotoViewModel(photo));
        }
    }
    ```

7. If the `Photos` collection already contains photos, get the timestamp of the first item (this will always be the newest so will have the largest timestamp), and only put photos that have a timestamp higher than this value into the collection. Adding to the collection puts items on the end, so you will need to `Insert` them at the start.

    ```cs
    else
    {
        var latest = Photos[0].Timestamp;
        foreach (var photo in photos.Where(p => p.Timestamp > latest).OrderBy(p => p.Timestamp))
        {
            Photos.Insert(0, new PhotoViewModel(photo));
        }
    }
    ```

All the new code for this class is shown below.

```cs
public MainViewModel()
{
    ...
    RefreshCommand = new Command(async () => await Refresh());
}

public ObservableCollection<PhotoViewModel> Photos { get; } = new ObservableCollection<PhotoViewModel>();
public ICommand RefreshCommand { get; }

async Task Refresh()
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
}
```

## Showing the photos on the page

### Adding a list view to show the photos

List views use data templates to specify how to show the items bound to the list. Out of the box there are some standard templates to show text or text and an image. To start with you can use a built in [`ImageCell`](https://docs.microsoft.com/dotnet/api/xamarin.forms.imagecell/?WT.mc_id=mobileappsoftomorrow-workshop-jabenn) as the template to view the photos. This UI will be improved later in this workshop.

1. Open the `MainPage.xaml` file.
2. Delete the `StackLayout` content from inside the `ContentPage`.
3. Add a `ListView` as the pages content, binding the `ItemsSource` to the photos collection on the view model.

    ```xml
     <ListView ItemsSource="{Binding Photos}">
    </ListView>
    ```

4. Set the item template for the list view to be an image cell. This needs to wrapped in a `DataTemplate`.

    ```xml
    <ListView.ItemTemplate>
        <DataTemplate>
            <ImageCell />
        </DataTemplate>
    </ListView.ItemTemplate>
    ```

5. The image cell has 3 properties of interest - `ImageSource`, `Text` and `Detail`. Image cells have the image on the left, with two rows of text next to it, the title on the top and detail underneath. You should bind these to the appropriate properties on the photo view model.

    ```xml
    <ImageCell ImageSource="{Binding Photo}"
               Text="{Binding Caption}"
               Detail="{Binding Tags}"/>
    ```

The full code for the contents of this page is below.

```xml
<ListView ItemsSource="{Binding Photos}">
    <ListView.ItemTemplate>
        <DataTemplate>
            <ImageCell ImageSource="{Binding Photo}"
                       Text="{Binding Caption}"
                       Detail="{Binding Tags}"/>
        </DataTemplate>
    </ListView.ItemTemplate>
</ListView>
```

### Refreshing the view model when the user logs in

When the main page is launched, it checks to see if the user is logged in, and if not shows the login page. After the user logs in your view model should load all the photos. To allow this to happen in a loosely coupled way your app can take advantage of the Xamarin.Forms [Messaging Center](https://docs.microsoft.com/xamarin/xamarin-forms/app-fundamentals/messaging-center/?WT.mc_id=mobileappsoftomorrow-workshop-jabenn). Classes can subscribe to messages from a particular source and react when these are received.

The Azure service can publish a message when the user logs in or when the user is successfully loaded from Application properties. The main view model can listen for this message and run the refresh accordingly.

1. Head to the `AzureServiceBase` and in the `Authenticate` method publish a message to the messaging center if the authentication was successful.

    ```cs
    public async Task<bool> Authenticate()
    {
        ...
        if (Client.CurrentUser != null)
        {
            MessagingCenter.Send<IAzureService>(this, "LoggedIn");
        ...
        }
        ...
    }

    ```

2. In the  `TryLoadUserDetails` method, publish the same message after the user is loaded from the Application properties.

    ```cs
    void TryLoadUserDetails()
    {
        ...
        Client.CurrentUser = new MobileServiceUser(userId.ToString())
        {
            MobileServiceAuthenticationToken = authToken.ToString()
        };

        MessagingCenter.Send<IAzureService>(this, "LoggedIn");
        ...
    }
    ```

3. In the constructor for the `MainViewModel` subscribe to the "LoggedIn" message, and when receive call the `Refresh` method.

    ```cs
    public MainViewModel()
    {
        MessagingCenter.Subscribe<IAzureService>(this, "LoggedIn", async s => await Refresh());
        ...
    }
    ```

### Test it out

You should now have everything in place to view the photos that have been uploaded. Run the app on your platform of choice and you will see the photos with their captions and tags.

> Like with uploading photos, this app doesn't include any feedback whilst the photos are being downloaded. In a production quality app you should provide feedback the download..

## Next step

Now that you have a basic UI to show photos, the next step is to [improve the UI](./13-ImproveTheUI.md).