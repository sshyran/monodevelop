//
// InformationAreaWidget.cs
//
// Author:
//       Mike Krüger <mikkrg@microsoft.com>
//
// Copyright (c) 2018 Microsoft Corporation. All rights reserved.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using Gtk;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Components.AtkCocoaHelper;
using MonoDevelop.Components;

namespace MonoDevelop.Ide.Gui
{
	public abstract partial class ViewContent
	{
		class InformationAreaWidget : HBox
		{
			IconId image;
			public IconId Image {
				get { return image; }
				set {
					image = value;
					UpdateImage ();
				}
			}

			ImageView img;
			HBox messageHBox = new HBox ();
			public InformationAreaWidget () : this (null)
			{
			}

			public InformationAreaWidget (IconId image)
			{
				BorderWidth = 6;
				Spacing = 6;
				ActionArea = new VButtonBox () {
					Spacing = 6,
					LayoutStyle = ButtonBoxStyle.Start
				};
				MessageArea = new Alignment (0f, 0f, 1f, 1f);

				this.PackEnd (ActionArea, false, false, 0);

				var messageVBox = new VBox ();

				messageHBox.PackEnd (MessageArea, true, true, 0);
				messageVBox.PackStart (messageHBox, false, false, 0);
				this.PackEnd (messageVBox, true, true, 0);
				this.image = image;
			}


			void UpdateImage ()
			{
				if (image.IsNull) {
					if (img != null) {
						this.Remove (img);
						img.Destroy ();
						img = null;
					}
					return;
				}

				if (img == null) {
					img = new ImageView (image, IconSize.Dialog);
					messageHBox.PackEnd (img, false, false, 12);
					img.SetAlignment (0.5f, 0.5f);
				} else {
					img.IconId = image;
				}
			}

			public ButtonBox ActionArea { get; private set; }
			public Container MessageArea { get; private set; }

			public void SetMessageLabel (string markup)
			{
				foreach (var child in MessageArea.Children) {
					MessageArea.Remove (child);
					child.Destroy ();
				}
				var l = new Gtk.Label () {
					Wrap = true,
					Selectable = true,
					Yalign = 0.5f,
					Xalign = 0f,
					Markup = markup,
					Style = Style
				};
				MessageArea.Add (l);
				l.SizeAllocated += delegate (object o, SizeAllocatedArgs args) {
					l.WidthRequest = Math.Max (350, args.Allocation.Width - 15);
				};
			}


			protected override bool OnExposeEvent (Gdk.EventExpose evnt)
			{
				Style.PaintFlatBox (Style, evnt.Window, StateType.Normal, ShadowType.Out, evnt.Area, this, "tooltip",
					Allocation.X + 1, Allocation.Y + 1, Allocation.Width - 2, Allocation.Height - 2);
				return base.OnExposeEvent (evnt);
			}

			protected override void OnSizeAllocated (Gdk.Rectangle allocation)
			{
				var rect = Allocation.Union (allocation);
				QueueDrawArea (rect.X, rect.Y, rect.Width, rect.Height);
				base.OnSizeAllocated (allocation);
			}

			//this is used to style like a tooltip
			bool changeStyle = false;

			protected override void OnStyleSet (Gtk.Style previous_style)
			{
				if (changeStyle)
					return;
				changeStyle = true;
				var surrogate = new TooltipStyleSurrogate ();
				surrogate.EnsureStyle ();
				this.Style = surrogate.Style;
				surrogate.Destroy ();
				foreach (var label in MessageArea.Children) {
					label.Style = Style;
				}

				base.OnStyleSet (previous_style);
				changeStyle = false;
			}

			class TooltipStyleSurrogate : TooltipWindow { }
		}
	}
}