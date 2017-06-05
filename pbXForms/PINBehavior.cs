using System.Text.RegularExpressions;
using Xamarin.Forms;

namespace pbXForms
{
	public class PINBehavior : Behavior<Entry>
	{
		protected override void OnAttachedTo(Entry entry)
		{
			entry.TextChanged += OnEntryTextChanged;
			base.OnAttachedTo(entry);
		}

		protected override void OnDetachingFrom(Entry entry)
		{
			entry.TextChanged -= OnEntryTextChanged;
			base.OnDetachingFrom(entry);
		}

		public string Text { get; private set; }

		void OnEntryTextChanged(object sender, TextChangedEventArgs args)
		{
			if (sender is Entry entry)
			{
				string newText = args.NewTextValue;
				if (string.IsNullOrEmpty(newText))
					Text = "";
				else
				{
					if (newText.Length < Text?.Length)
						Text = Text.Substring(0, newText.Length);
					else
					{
						string last = newText.Substring(newText.Length - 1);
						const string dot = "•";
						if (last != dot)
						{
							if (Regex.IsMatch(last, "[0-9]"))
							{
								Text += last;
								entry.Text = dot.PadRight(Text.Length, dot[0]);
							}
							else
								entry.Text = newText.Substring(0, newText.Length - 1);
						}
					}
				}
			}
		}
	}
}