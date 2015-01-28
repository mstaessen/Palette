using System;
using System.Linq;
using System.Threading.Tasks;
using Android.Support.V7.Widget;
using Android.Views;
using Microsoft.WindowsAzure.MobileServices.Sync;

namespace XamarinPalette
{
	public class RecyclerViewAdapter<T> : RecyclerView.Adapter
	{
		private readonly IMobileServiceSyncTable<T> table;
		private readonly int itemLayout;
		private readonly Action<T, View> bindAction;
	    private int itemCount = 0;

	    public RecyclerViewAdapter (IMobileServiceSyncTable<T> table, int itemLayout, Action<T, View> bindAction = null)
		{
			if (table == null) {
				throw new ArgumentNullException ("table");
			}
			this.table = table;
			this.itemLayout = itemLayout;
			this.bindAction = bindAction;
		}

	    public async Task Initialize()
	    {
	        itemCount = (await table.CreateQuery().ToEnumerableAsync()).Count();
	    }

		public override async void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
		{
			if (bindAction != null) {
			    var result = await table.CreateQuery().Skip(position).Take(1).ToListAsync(); 
			    var item = result.FirstOrDefault();
			    if (item != null) {
                    bindAction(item, holder.ItemView);
			    }
			}
		}

		public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
		{
			var v = LayoutInflater.From(parent.Context).Inflate(itemLayout, parent, false);
			return new ViewHolder(v);
		}

		public override int ItemCount {
			get { return itemCount; }
		}

		public async void Add(T item) {
			await table.InsertAsync(item);
		    itemCount++;
			NotifyItemInserted (ItemCount - 1);
		}

		public async void Remove(T item)
		{
		    await table.DeleteAsync(item);
		    itemCount--;
			NotifyDataSetChanged();
		}
	}

	public class ViewHolder : RecyclerView.ViewHolder {
		public ViewHolder (View v) : base(v) { }
	}
}