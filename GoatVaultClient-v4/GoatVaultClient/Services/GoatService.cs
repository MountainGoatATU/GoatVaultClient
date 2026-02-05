using Microsoft.Maui.Controls;
using SkiaSharp.Extended.UI.Controls;

namespace GoatVaultClient.Services
{
    public class GoatService
    {
        private readonly AbsoluteLayout _container;
        private readonly SKLottieView _goat;
        private readonly StackLayout _bubble;
        private readonly Label _label;

        public GoatService(AbsoluteLayout container, SKLottieView goat, StackLayout bubble)
        {
            _container = container;
            _goat = goat;
            _bubble = bubble;
            _label = bubble.Children[0] is Border border && border.Content is Label lbl ? lbl : throw new Exception("Bubble label not found");

            // start hidden
            _container.Opacity = 0;
        }

        public void ShowGoat(string comment)
        {
            _label.Text = comment;
            _container.Opacity = 1;
        }

        public void HideGoat()
        {
            _container.Opacity = 0;
        }
    }
}
