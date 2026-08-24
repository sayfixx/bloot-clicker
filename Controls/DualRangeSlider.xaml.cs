using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;

namespace Autoclicker.Controls
{

	public partial class DualRangeSlider : UserControl
	{
		public static readonly DependencyProperty MinimumProperty =
			DependencyProperty.Register(nameof(Minimum), typeof(double), typeof(DualRangeSlider),
				new PropertyMetadata(1d, OnRangeBoundsChanged));

		public static readonly DependencyProperty MaximumProperty =
			DependencyProperty.Register(nameof(Maximum), typeof(double), typeof(DualRangeSlider),
				new PropertyMetadata(25d, OnRangeBoundsChanged));

		public static readonly DependencyProperty LowerValueProperty =
			DependencyProperty.Register(nameof(LowerValue), typeof(double), typeof(DualRangeSlider),
				new FrameworkPropertyMetadata(12d, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
					OnLowerValueChanged, CoerceLowerValue));

		public static readonly DependencyProperty UpperValueProperty =
			DependencyProperty.Register(nameof(UpperValue), typeof(double), typeof(DualRangeSlider),
				new FrameworkPropertyMetadata(18d, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
					OnUpperValueChanged, CoerceUpperValue));

		public static readonly DependencyProperty MinimumGapProperty =
			DependencyProperty.Register(nameof(MinimumGap), typeof(double), typeof(DualRangeSlider),
				new PropertyMetadata(1d));

		public static readonly DependencyProperty IsIntegerProperty =
			DependencyProperty.Register(nameof(IsInteger), typeof(bool), typeof(DualRangeSlider),
				new PropertyMetadata(true));

		public static readonly DependencyProperty AccentBrushProperty =
			DependencyProperty.Register(nameof(AccentBrush), typeof(Brush), typeof(DualRangeSlider),
				new PropertyMetadata(new SolidColorBrush(Color.FromRgb(0xFF, 0x33, 0xE6))));

		public static readonly DependencyProperty AccentColorProperty =
			DependencyProperty.Register(nameof(AccentColor), typeof(Color), typeof(DualRangeSlider),
				new PropertyMetadata(Color.FromRgb(0xFF, 0x33, 0xE6)));

		public double Minimum
		{
			get => (double)GetValue(MinimumProperty);
			set => SetValue(MinimumProperty, value);
		}

		public double Maximum
		{
			get => (double)GetValue(MaximumProperty);
			set => SetValue(MaximumProperty, value);
		}

		public double LowerValue
		{
			get => (double)GetValue(LowerValueProperty);
			set => SetValue(LowerValueProperty, value);
		}

		public double UpperValue
		{
			get => (double)GetValue(UpperValueProperty);
			set => SetValue(UpperValueProperty, value);
		}

		public double MinimumGap
		{
			get => (double)GetValue(MinimumGapProperty);
			set => SetValue(MinimumGapProperty, value);
		}

		public bool IsInteger
		{
			get => (bool)GetValue(IsIntegerProperty);
			set => SetValue(IsIntegerProperty, value);
		}

		public Brush AccentBrush
		{
			get => (Brush)GetValue(AccentBrushProperty);
			set => SetValue(AccentBrushProperty, value);
		}

		public Color AccentColor
		{
			get => (Color)GetValue(AccentColorProperty);
			set => SetValue(AccentColorProperty, value);
		}

		public event EventHandler RangeChanged;

		public DualRangeSlider()
		{
			InitializeComponent();
			Loaded += (_, __) =>
			{
				UpdateThumbPositions();
				SyncVisualPositions();
			};
		}

		private static void OnRangeBoundsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var slider = (DualRangeSlider)d;
			slider.CoerceValue(LowerValueProperty);
			slider.CoerceValue(UpperValueProperty);
			slider.UpdateThumbPositions();
		}

		private static object CoerceLowerValue(DependencyObject d, object baseValue)
		{
			var slider = (DualRangeSlider)d;
			double value = slider.Clamp((double)baseValue, slider.Minimum, slider.Maximum);

			double maxAllowed = slider.UpperValue - slider.MinimumGap;
			if (value > maxAllowed)
			{
				value = Math.Max(slider.Minimum, maxAllowed);
			}

			return slider.Snap(value);
		}

		private static object CoerceUpperValue(DependencyObject d, object baseValue)
		{
			var slider = (DualRangeSlider)d;
			double value = slider.Clamp((double)baseValue, slider.Minimum, slider.Maximum);

			double minAllowed = slider.LowerValue + slider.MinimumGap;
			if (value < minAllowed)
			{
				value = Math.Min(slider.Maximum, minAllowed);
			}

			return slider.Snap(value);
		}

		private static void OnLowerValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var slider = (DualRangeSlider)d;
			slider.CoerceValue(UpperValueProperty);
			slider.UpdateThumbPositions();
			slider.RangeChanged?.Invoke(slider, EventArgs.Empty);
		}

		private static void OnUpperValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var slider = (DualRangeSlider)d;
			slider.CoerceValue(LowerValueProperty);
			slider.UpdateThumbPositions();
			slider.RangeChanged?.Invoke(slider, EventArgs.Empty);
		}

		private double Clamp(double value, double min, double max)
		{
			if (max < min)
			{
				(min, max) = (max, min);
			}
			return Math.Max(min, Math.Min(max, value));
		}

		private double Snap(double value)
		{
			return IsInteger ? Math.Round(value, MidpointRounding.AwayFromZero) : value;
		}

		private const double ThumbHaloOverflow = 16.5;

		private double _lowerVisualLeft;
		private double _upperVisualLeft;
		private double _lowerTargetLeft;
		private double _upperTargetLeft;
		private bool _lowerDragging;
		private bool _upperDragging;
		private bool _renderingHooked;

		private void EnsureRenderingHooked()
		{
			if (_renderingHooked)
			{
				return;
			}

			CompositionTarget.Rendering += OnRendering;
			_renderingHooked = true;
		}

		private void OnRendering(object sender, EventArgs e)
		{
			if (LowerThumb == null || UpperThumb == null)
			{
				return;
			}

			bool changed = false;

			if (_lowerDragging)
			{
				_lowerVisualLeft = SmoothTowards(_lowerVisualLeft, _lowerTargetLeft);
				Canvas.SetLeft(LowerThumb, _lowerVisualLeft);
				changed = true;
			}

			if (_upperDragging)
			{
				_upperVisualLeft = SmoothTowards(_upperVisualLeft, _upperTargetLeft);
				Canvas.SetLeft(UpperThumb, _upperVisualLeft);
				changed = true;
			}

			if (!changed)
			{
				CompositionTarget.Rendering -= OnRendering;
				_renderingHooked = false;
			}
		}

		private static double SmoothTowards(double current, double target)
		{

			double next = current + (target - current) * 0.75;

			if (Math.Abs(target - next) < 0.05)
			{
				return target;
			}

			return next;
		}

		private void RootGrid_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			UpdateThumbPositions();
		}

		private void UpdateThumbPositions()
		{
			if (LowerThumb == null || UpperThumb == null || RootGrid == null)
			{
				return;
			}

			double range = Maximum - Minimum;
			if (range <= 0 || RootGrid.ActualWidth <= 0)
			{
				return;
			}

			if (_lowerDragging || _upperDragging)
			{
				return;
			}

			double usableWidth = Math.Max(0, RootGrid.ActualWidth - LowerThumb.Width - 2 * ThumbHaloOverflow);

			double lowerRatio = (LowerValue - Minimum) / range;
			double upperRatio = (UpperValue - Minimum) / range;

			double lowerLeft = ThumbHaloOverflow + lowerRatio * usableWidth;
			double upperLeft = ThumbHaloOverflow + upperRatio * usableWidth;

			_lowerVisualLeft = _lowerTargetLeft = lowerLeft;
			_upperVisualLeft = _upperTargetLeft = upperLeft;

			Canvas.SetLeft(LowerThumb, lowerLeft);
			Canvas.SetLeft(UpperThumb, upperLeft);
		}

		private void SyncVisualPositions()
		{
			if (LowerThumb == null || UpperThumb == null)
			{
				return;
			}

			_lowerVisualLeft = _lowerTargetLeft = Canvas.GetLeft(LowerThumb);
			_upperVisualLeft = _upperTargetLeft = Canvas.GetLeft(UpperThumb);
		}

		private void Thumb_DragStarted(object sender, DragStartedEventArgs e)
		{
			if (ReferenceEquals(sender, LowerThumb))
			{
				_lowerDragging = true;
				_lowerVisualLeft = Canvas.GetLeft(LowerThumb);
				_lowerTargetLeft = _lowerVisualLeft;
			}
			else if (ReferenceEquals(sender, UpperThumb))
			{
				_upperDragging = true;
				_upperVisualLeft = Canvas.GetLeft(UpperThumb);
				_upperTargetLeft = _upperVisualLeft;
			}

			EnsureRenderingHooked();
		}

		private void Thumb_DragCompleted(object sender, DragCompletedEventArgs e)
		{
			if (ReferenceEquals(sender, LowerThumb))
			{
				_lowerDragging = false;
				_lowerVisualLeft = _lowerTargetLeft;
				Canvas.SetLeft(LowerThumb, _lowerVisualLeft);
			}
			else if (ReferenceEquals(sender, UpperThumb))
			{
				_upperDragging = false;
				_upperVisualLeft = _upperTargetLeft;
				Canvas.SetLeft(UpperThumb, _upperVisualLeft);
			}

			UpdateThumbPositions();
		}

		private void LowerThumb_DragDelta(object sender, DragDeltaEventArgs e)
		{
			ApplyDrag(LowerThumb, e.HorizontalChange, isLower: true);
		}

		private void UpperThumb_DragDelta(object sender, DragDeltaEventArgs e)
		{
			ApplyDrag(UpperThumb, e.HorizontalChange, isLower: false);
		}

		private void ApplyDrag(Thumb thumb, double horizontalChange, bool isLower)
		{
			double usableWidth = Math.Max(1, RootGrid.ActualWidth - thumb.Width - 2 * ThumbHaloOverflow);
			double minLeft = ThumbHaloOverflow;
			double maxLeft = ThumbHaloOverflow + usableWidth;
			double range = Maximum - Minimum;

			ref double targetLeft = ref (isLower ? ref _lowerTargetLeft : ref _upperTargetLeft);
			double newLeft = Math.Max(minLeft, Math.Min(maxLeft, targetLeft + horizontalChange));

			double gapPixels = MinimumGap / Math.Max(range, 0.000001) * usableWidth;

			double otherLeft = isLower
				? (_upperDragging ? _upperTargetLeft : _upperVisualLeft)
				: (_lowerDragging ? _lowerTargetLeft : _lowerVisualLeft);

			if (isLower)
			{
				newLeft = Math.Min(newLeft, otherLeft - gapPixels);
			}
			else
			{
				newLeft = Math.Max(newLeft, otherLeft + gapPixels);
			}

			newLeft = Math.Max(minLeft, Math.Min(maxLeft, newLeft));
			targetLeft = newLeft;

			double ratio = (newLeft - minLeft) / usableWidth;
			double rawValue = Minimum + ratio * range;

			if (isLower)
			{
				LowerValue = rawValue;
			}
			else
			{
				UpperValue = rawValue;
			}

			EnsureRenderingHooked();
		}
	}
}
