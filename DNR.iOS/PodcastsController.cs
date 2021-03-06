using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using DNR.Portable;
using DNR.Portable.Models;
using DNR.Portable.Services;
using Microsoft.WindowsAzure.MobileServices;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using System.Drawing;

namespace DNR
{
  public class PodcastsController : UICollectionViewController
  {
    private static PodcastsViewModel viewModel;

    public static PodcastsViewModel ViewModel
    {
      get { return viewModel ?? (viewModel = new PodcastsViewModel()); }
    }
    
    PodcastDetailController podcastController;
    UISearchBar searchBar;
    UIActivityIndicatorView activityView;

    public PodcastsController(UICollectionViewLayout layout)
      : base(layout)
    {

      searchBar = new UISearchBar
      {
        Placeholder = "Search for a podcast",
        AutocorrectionType = UITextAutocorrectionType.No,
        AutocapitalizationType = UITextAutocapitalizationType.None,
        AutoresizingMask = UIViewAutoresizing.All,
        Alpha = 0.4f
      };

      searchBar.SizeToFit();

      searchBar.SearchButtonClicked += (sender, e) =>
      {
        Search(searchBar.Text);
        searchBar.ResignFirstResponder();
      };

      searchBar.TextChanged += (sender, e) => Search(e.SearchText);
    }

    public override void ViewDidLoad()
    {
      base.ViewDidLoad();

      AddActivityIndicator();

      CollectionView.BackgroundColor = UIColor.LightGray;
      CollectionView.RegisterNibForCell(PodcastCell.Nib, PodcastCell.Key);
      NavigationController.NavigationBar.Add(searchBar);
      NavigationController.NavigationBar.BackgroundColor = UIColor.Black;
      GetEpisodes();
    }

    async void GetEpisodes()
    {
      // Comment this out if you do not want Twitter Authentication
      // You will need to set your Azure permissions to any client with API Key
      await Authenticate();

      activityView.StartAnimating();

      await ViewModel.ExecuteGetPodcastsCommand();

      CollectionView.ReloadData();
      activityView.StopAnimating();
    }

    public override void ItemSelected(UICollectionView collectionView, NSIndexPath indexPath)
    {
      if (podcastController == null)
      {
        podcastController = new PodcastDetailController();
      }

      searchBar.Hidden = true;
      Title = ".NET Rocks";

      podcastController.CurrentPodcastEpisode = ViewModel.FilteredPodcasts[indexPath.Row];
      NavigationController.PushViewController(podcastController, true);
    }



    public override void ViewWillAppear(bool animated)
    {
      base.ViewWillAppear(animated);

      Title = string.Empty;
      searchBar.Hidden = false;

      var orientation = UIDevice.CurrentDevice.Orientation;

      if (orientation == UIDeviceOrientation.LandscapeLeft || orientation == UIDeviceOrientation.LandscapeRight)
        SetLayout(false);
      else
        SetLayout(true);
    }

    public override void ViewWillDisappear(bool animated)
    {
      base.ViewWillDisappear(animated);

      searchBar.ResignFirstResponder();
    }

   

    public override int GetItemsCount(UICollectionView collectionView, int section)
    {
      return ViewModel.FilteredPodcasts.Count;
    }

    public override UICollectionViewCell GetCell(UICollectionView collectionView, NSIndexPath indexPath)
    {
      var podcastCell = (PodcastCell)collectionView.DequeueReusableCell(PodcastCell.Key, indexPath);
      podcastCell.Name = ViewModel.FilteredPodcasts[indexPath.Row].Name;
      podcastCell.DetailText = ViewModel.FilteredPodcasts[indexPath.Row].Description;

      return podcastCell;
    }

    void AddActivityIndicator()
    {
      activityView = new UIActivityIndicatorView
      {
        Frame = new RectangleF(0, 0, 50, 50),
        Center = View.Center,
        ActivityIndicatorViewStyle = UIActivityIndicatorViewStyle.WhiteLarge
      };

      View.AddSubview(activityView);
    }

    public override void WillRotate(UIInterfaceOrientation toInterfaceOrientation, double duration)
    {
      base.WillRotate(toInterfaceOrientation, duration);

      if (toInterfaceOrientation == UIInterfaceOrientation.LandscapeLeft || toInterfaceOrientation == UIInterfaceOrientation.LandscapeRight)
        SetLayout(false);
      else
        SetLayout(true);
    }

    void SetLayout(bool isPortrait)
    {
      var layout = new UICollectionViewFlowLayout
      {
        MinimumInteritemSpacing = 5.0f,
        MinimumLineSpacing = 5.0f,
        SectionInset = new UIEdgeInsets(5, 5, 5, 5),
        ItemSize = isPortrait ? PodcastCell.PortraitSize : PodcastCell.LandscapeSize
      };

      CollectionView.SetCollectionViewLayout(layout, false);
    }

    void Search(string text)
    {
      ViewModel.FilterPodcastCommand.Execute(text);
      CollectionView.ReloadData();
    }


    private async System.Threading.Tasks.Task Authenticate()
    {
      while (AzureWebService.Instance.Client.CurrentUser == null)
      {
        string message;
        try
        {

          AzureWebService.Instance.Client.CurrentUser = await AzureWebService.Instance.Client
            .LoginAsync(this, MobileServiceAuthenticationProvider.Twitter);
          message =
            string.Format("You are now logged in - {0}", AzureWebService.Instance.Client.CurrentUser.UserId);


        }
        catch (InvalidOperationException ex)
        {
          message = "You must log in. Login Required";
        }

        var alert = new UIAlertView("Login", message, null, "OK", null);
        alert.Show();
      }
    }
  }
}