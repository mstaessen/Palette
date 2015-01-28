using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Support.V7.Graphics;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using Microsoft.WindowsAzure.MobileServices;
using Microsoft.WindowsAzure.MobileServices.SQLiteStore;
using Microsoft.WindowsAzure.MobileServices.Sync;
using XamarinPalette.Domain;
using AndroidUri = Android.Net.Uri;

namespace XamarinPalette.Activities
{
    [Activity(Label = "Palette Xamarin", MainLauncher = true, Icon = "@drawable/icon", Theme = "@style/AppTheme")]
    public class MainActivity : Activity, Palette.IPaletteAsyncListener
    {
        private const int ActionPickImage = 1;
        private static int paletteId;

        private IList<ColorPalette> colorPalettes;
        private RecyclerViewAdapter<ColorPalette> colorPaletteAdapter;

        private IMobileServiceClient client;
        private IMobileServiceSyncTable<ColorPalette> colorPaletteTable;
        private const string LocalDbFilename = "database.db";

        protected override async void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.Main);

            client = new MobileServiceClient("https://palette.azure-mobile.net/", "qINlajvMMHWpIVCiudiplOvWNEXCFb26");
            await InitLocalStoreAsync();

            colorPaletteTable = client.GetSyncTable<ColorPalette>();
            colorPaletteAdapter = new RecyclerViewAdapter<ColorPalette>(colorPaletteTable, Resource.Layout.Palette, OnBind);
            await colorPaletteAdapter.Initialize();

            var paletteList = FindViewById<RecyclerView>(Resource.Id.list_palette);
            paletteList.SetItemAnimator(new DefaultItemAnimator());
            paletteList.SetAdapter(colorPaletteAdapter);
            paletteList.SetLayoutManager(new LinearLayoutManager(this));

            var addButton = FindViewById<Button>(Resource.Id.button_add_palette);
            addButton.Click += OnClickAddPalette;
        }

        private void OnBind(ColorPalette palette, View view)
        {
            var textView = view.FindViewById<TextView>(Resource.Id.text_palette_id);
            if (textView != null) {
                textView.Text = "Palette " + palette.Id;
            }

            var vibrantView = view.FindViewById<View>(Resource.Id.palette_vibrant);
            if (vibrantView != null) {
                vibrantView.SetBackgroundColor(new Color(palette.Vibrant));
            }

            View vibrantDarkView = view.FindViewById<View>(Resource.Id.palette_vibrant_dark);
            if (vibrantDarkView != null) {
                vibrantDarkView.SetBackgroundColor(new Color(palette.DarkVibrant));
            }

            View vibrantLightView = view.FindViewById<View>(Resource.Id.palette_vibrant_light);
            if (vibrantLightView != null) {
                vibrantLightView.SetBackgroundColor(new Color(palette.LightVibrant));
            }

            View mutedView = view.FindViewById<View>(Resource.Id.palette_muted);
            if (mutedView != null) {
                mutedView.SetBackgroundColor(new Color(palette.Muted));
            }

            View mutedDarkView = view.FindViewById<View>(Resource.Id.palette_muted_dark);
            if (mutedDarkView != null) {
                mutedDarkView.SetBackgroundColor(new Color(palette.DarkMuted));
            }

            View mutedLightView = view.FindViewById<View>(Resource.Id.palette_muted_light);
            if (mutedLightView != null) {
                mutedLightView.SetBackgroundColor(new Color(palette.LightMuted));
            }
        }

        private Bitmap GetBitmapFromUri(AndroidUri uri)
        {
            using (var parcelFileDescriptor = ContentResolver.OpenFileDescriptor(uri, "r")) {
                return BitmapFactory.DecodeFileDescriptor(parcelFileDescriptor.FileDescriptor);
            }
        }

        private void OnClickAddPalette(object sender, EventArgs e)
        {
            var intent = new Intent(Intent.ActionOpenDocument);
            intent.AddCategory(Intent.CategoryOpenable);
            intent.SetType("image/*");
            StartActivityForResult(Intent.CreateChooser(intent, "Select Picture"), ActionPickImage);
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);
            if (requestCode == ActionPickImage && resultCode == Result.Ok) {
                if (data != null) {
                    var uri = data.Data;
                    var bitmap = GetBitmapFromUri(uri);
                    Palette.GenerateAsync(bitmap, this);
                }
            }
        }

        public void OnGenerated(Palette palette)
        {
            colorPaletteAdapter.Add(new ColorPalette {
                Id = paletteId++,
                Vibrant = palette.GetVibrantColor(Color.White),
                DarkVibrant = palette.GetDarkVibrantColor(Color.White),
                LightVibrant = palette.GetLightVibrantColor(Color.White),
                Muted = palette.GetMutedColor(Color.White),
                DarkMuted = palette.GetDarkMutedColor(Color.White),
                LightMuted = palette.GetLightMutedColor(Color.White)
            });
        }

        private async Task InitLocalStoreAsync()
        {
            // new code to initialize the SQLite store
            var path = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), LocalDbFilename);

            if (!File.Exists(path)) {
                File.Create(path).Dispose();
            }

            var store = new MobileServiceSQLiteStore(path);
            store.DefineTable<ColorPalette>();

            // Uses the default conflict handler, which fails on conflict
            await client.SyncContext.InitializeAsync(store);
        }

        private async Task SyncAsync()
        {
            await client.SyncContext.PushAsync();
            await colorPaletteTable.PullAsync("allColorPalettes", colorPaletteTable.CreateQuery());
                // query ID is used for incremental sync
        }
    }
}