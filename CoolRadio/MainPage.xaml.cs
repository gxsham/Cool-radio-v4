using Microsoft.Toolkit.Uwp.UI.Animations;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace CoolRadio
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class MainPage : Page
	{
		private bool isPlaying { get; set; }
		private HttpClient client { get; set; }
		private StorageFolder localFolder = ApplicationData.Current.LocalFolder;
		private string actualSong { get; set; }
		private ObservableCollection<Song> songs { get; set; }
		private const string CheckTrackLink = "https://api.radioking.fr/radio/accounts/getcurrenttitle?callback=jQuery17101469612929121742_1500812158616&apikey=90584ec6691c7c84af230897a67c5144&hash=a3ed7c59cf8c89397a22d620f819ac5c2f50daba&timestamp=1500812158&username=my-radio&idserveur=8&host=coolradio.md&port=8000&radiouid=&mount=profm-128.mp3&url=&typetitrage=icecast&app=playerrkajax";
		public MainPage()
		{
			this.InitializeComponent();
			client = new HttpClient();
			CheckTrack();
			isPlaying = true;
			songs = new ObservableCollection<Song>();
			GetSongs();
			listView.ItemsSource = songs;
		}

		private async void Grid_PointerEntered(object sender, PointerRoutedEventArgs e)
		{
			await img.Blur(duration: 200, delay: 0, value: 4).StartAsync();
			play.Visibility = Visibility.Visible;
		}

		private async void Grid_PointerExited(object sender, PointerRoutedEventArgs e)
		{
			await img.Blur(duration: 200, delay: 0, value: 0).StartAsync();
			play.Visibility = Visibility.Collapsed;
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			if (isPlaying == true)
			{
				mediaElement.Pause();
				isPlaying = false;
				play.Source = new Windows.UI.Xaml.Media.Imaging.BitmapImage(new Uri("ms-appx:///Assets/PinkCircle.png", UriKind.RelativeOrAbsolute));
			}
			else
			{
				mediaElement.Play();
				isPlaying = true;
				play.Source = new Windows.UI.Xaml.Media.Imaging.BitmapImage(new Uri("ms-appx:///Assets/WhiteCircle.png", UriKind.RelativeOrAbsolute));
			}
		}

		private async void CheckTrack()
		{
			string lastSong = string.Empty;
			JObject obj = null;
			string artist = string.Empty;
			string title = string.Empty;
			while (true)
			{
				obj = GetTrack();
				if (obj != null)
				{
					artist = (string)obj["results"]?["artist"] ?? "";
					title = (string)obj["results"]?["title"] ?? "";
					if (lastSong != artist + title)
					{
						Artist.Text = artist;
						Title.Text = title;
						var preparedLink = PrepareLink(artist, title);
						var coverUrl = GetCover(preparedLink);
						if (!string.IsNullOrEmpty(coverUrl))
						{
							img.Source = new Windows.UI.Xaml.Media.Imaging.BitmapImage(new Uri(coverUrl, UriKind.RelativeOrAbsolute));
						}
						else
						{
							img.Source = new Windows.UI.Xaml.Media.Imaging.BitmapImage(new Uri("https://unsplash.it/600?random", UriKind.RelativeOrAbsolute));
						}
					}
					actualSong = $"{artist} - {title}\n";
					lastSong = artist + title;
					await Task.Delay(10000);
				}
			}
		}

		private JObject GetTrack()
		{
			try
			{
				var result = client.GetStringAsync(CheckTrackLink).Result;
				int start = result.IndexOf("(") + 1;
				int end = result.LastIndexOf(")") - start;
				result = result.Substring(start, end);
				return JObject.Parse(result);
			}
			catch
			{
				return null;
			}

		}

		private string GetCover(string link)
		{
			var result = client.GetStringAsync(link);
			try
			{
				var obj = JObject.Parse(result.Result);
				return (string)obj["cover_url"] ?? "";
			}
			catch (Exception)
			{
				return string.Empty;
			}
		}
		private string PrepareLink(string artist, string title)
		{
			var linkArtist = artist.Split(new Char[] { ' ', ',', '.', ':', '\t' }).Aggregate((a, b) => a + '+' + b);
			var linkTitle = title.Split(new Char[] { ' ', ',', '.', ':', '\t' }).Aggregate((a, b) => a + '+' + b);
			return $"https://www.radioking.com/api/meta/v2/track?key=088d46bbdf7eabbe9f8c88fad06c779d013b5a53&artist={linkArtist}&title={linkTitle}";
		}

		private async void SaveSong_Click(object sender, RoutedEventArgs e)
		{
			StorageFile storageFile	= await localFolder.GetFileAsync("savedSongs.txt");
			if (storageFile == null)
			{
				storageFile = await localFolder.CreateFileAsync("savedSongs.txt");
			}
			await FileIO.AppendTextAsync(storageFile, actualSong);
			songs.Add(new Song {Name = actualSong });
		}

		private async void Delete_Click(object sender, RoutedEventArgs e)
		{
			Button btn = (Button)sender;
			var toRemove = (Song)btn.DataContext;
			songs.Remove(toRemove);
			try
			{
				StorageFile storageFile = await localFolder.CreateFileAsync("savedSongs.txt",CreationCollisionOption.ReplaceExisting);
				foreach (var item in songs)
				{
					await FileIO.AppendTextAsync(storageFile,item.Name);
				}			
			}
			catch { }
		}

		private async void GetSongs()
		{
			var result = new ObservableCollection<Song>();
			try
			{
				StorageFile storageFile = await localFolder.GetFileAsync("savedSongs.txt");
				var lines = await FileIO.ReadLinesAsync(storageFile);
				if (lines.Count == 0)
				{
					songs.Add(new Song { Name = "Save songs that you liked!" });
				}
				else
				{
					foreach (var item in lines)
					{
						songs.Add(new Song { Name = item });
					}
				}
			}
			catch { }
		}
	}
}