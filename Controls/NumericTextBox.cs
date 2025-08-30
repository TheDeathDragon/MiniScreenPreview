using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MiniScreenPreview.Controls
{
    public class NumericTextBox : TextBox
    {
        public static readonly DependencyProperty StepProperty =
            DependencyProperty.Register(
                nameof(Step),
                typeof(double),
                typeof(NumericTextBox),
                new PropertyMetadata(1.0));

        public static readonly DependencyProperty MinimumProperty =
            DependencyProperty.Register(
                nameof(Minimum),
                typeof(double),
                typeof(NumericTextBox),
                new PropertyMetadata(double.MinValue));

        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register(
                nameof(Maximum),
                typeof(double),
                typeof(NumericTextBox),
                new PropertyMetadata(double.MaxValue));

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(
                nameof(Value),
                typeof(double),
                typeof(NumericTextBox),
                new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValueChanged));

        public static readonly DependencyProperty FormatModeProperty =
            DependencyProperty.Register(
                nameof(FormatMode),
                typeof(string),
                typeof(NumericTextBox),
                new PropertyMetadata(""));

        public double Step
        {
            get => (double)GetValue(StepProperty);
            set => SetValue(StepProperty, value);
        }

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

        public double Value
        {
            get => (double)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public string FormatMode
        {
            get => (string)GetValue(FormatModeProperty);
            set => SetValue(FormatModeProperty, value);
        }

        public NumericTextBox()
        {
            Loaded += OnLoaded;
            MouseWheel += OnMouseWheel;
            MouseEnter += OnMouseEnter;
            MouseLeave += OnMouseLeave;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Text = FormatValue(Value);
        }

        private void OnMouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            ToolTip = $"Scroll wheel: ±{Step} | Arrow keys: ±{Step} | Enter: Apply";
        }

        private void OnMouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            ToolTip = null;
        }

        private void OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (IsMouseOver)
            {
                var currentValue = Value;
                var step = Step;
                var delta = e.Delta > 0 ? step : -step;

                Value = Math.Max(Minimum, Math.Min(Maximum, currentValue + delta));

                Text = FormatValue(Value);

                if (IsFocused)
                {
                    SelectAll();
                }

                e.Handled = true;
            }
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is NumericTextBox textBox)
            {
                textBox.Text = textBox.FormatValue((double)e.NewValue);
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Return)
            {
                ApplyTextValue();
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Up || e.Key == Key.Down || e.Key == Key.Left || e.Key == Key.Right)
            {
                var currentValue = Value;
                var step = Step;

                switch (e.Key)
                {
                    case Key.Up:
                    case Key.Right:
                        Value = Math.Max(Minimum, Math.Min(Maximum, currentValue + step));
                        break;
                    case Key.Down:
                    case Key.Left:
                        Value = Math.Max(Minimum, Math.Min(Maximum, currentValue - step));
                        break;
                }

                Text = FormatValue(Value);
                SelectAll();

                e.Handled = true;
                return;
            }

            base.OnKeyDown(e);
        }

        protected override void OnLostFocus(RoutedEventArgs e)
        {
            ApplyTextValue();
            base.OnLostFocus(e);
        }

        private void ApplyTextValue()
        {
            if (double.TryParse(Text, out var newValue))
            {
                if (FormatMode == "percent")
                {
                    newValue = newValue / 100.0;
                }
                Value = Math.Max(Minimum, Math.Min(Maximum, newValue));
            }
            else
            {
                Text = FormatValue(Value);
            }
        }

        private string FormatValue(double value)
        {
            if (FormatMode == "percent")
            {
                return (value * 100).ToString("F0", CultureInfo.InvariantCulture);
            }
            else if (Step < 1.0)
            {
                var decimalPlaces = Math.Max(0, -(int)Math.Floor(Math.Log10(Step)));
                return value.ToString($"F{decimalPlaces}", CultureInfo.InvariantCulture);
            }
            else
            {
                return value.ToString("F0", CultureInfo.InvariantCulture);
            }
        }
    }
}