using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace RPG_Game_Elfshock.Controls
{
    /// <summary>
    /// A ContentControl that plays a short fade + slide-up animation whenever its
    /// content changes, giving a smooth transition between screens (menus).
    /// </summary>
    public class TransitioningContentControl : ContentControl
    {
        private readonly TranslateTransform _slide = new TranslateTransform();

        public TransitioningContentControl()
        {
            RenderTransform = _slide;
        }

        protected override void OnContentChanged(object oldContent, object newContent)
        {
            base.OnContentChanged(oldContent, newContent);

            if (newContent is null)
                return;

            var duration = new Duration(TimeSpan.FromMilliseconds(250));
            var ease = new CubicEase { EasingMode = EasingMode.EaseOut };

            // Fade the new screen in from transparent.
            var fade = new DoubleAnimation(0.0, 1.0, duration) { EasingFunction = ease };

            // Slide it up slightly into place.
            var slide = new DoubleAnimation(18.0, 0.0, duration) { EasingFunction = ease };

            BeginAnimation(OpacityProperty, fade);
            _slide.BeginAnimation(TranslateTransform.YProperty, slide);
        }
    }
}
