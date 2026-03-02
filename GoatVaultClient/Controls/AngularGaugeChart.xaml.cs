namespace GoatVaultClient.Controls;

public partial class AngularGaugeView
{
    public AngularGaugeView()
    {
        InitializeComponent();
    }

    public static readonly BindableProperty ValueProperty =
        BindableProperty.Create(nameof(Value), typeof(double), typeof(AngularGaugeView), 0d);

    public double Value
    {
        get => (double)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public static readonly BindableProperty Segment1Property =
        BindableProperty.Create(nameof(Segment1), typeof(double), typeof(AngularGaugeView), 60d);

    public double Segment1
    {
        get => (double)GetValue(Segment1Property);
        set => SetValue(Segment1Property, value);
    }

    public static readonly BindableProperty Segment2Property =
        BindableProperty.Create(nameof(Segment2), typeof(double), typeof(AngularGaugeView), 30d);

    public double Segment2
    {
        get => (double)GetValue(Segment2Property);
        set => SetValue(Segment2Property, value);
    }

    public static readonly BindableProperty Segment3Property =
        BindableProperty.Create(nameof(Segment3), typeof(double), typeof(AngularGaugeView), 10d);

    public double Segment3
    {
        get => (double)GetValue(Segment3Property);
        set => SetValue(Segment3Property, value);
    }

    public static readonly BindableProperty MinValueProperty =
        BindableProperty.Create(nameof(MinValue), typeof(double), typeof(AngularGaugeView), 0d);

    public double MinValue
    {
        get => (double)GetValue(MinValueProperty);
        set => SetValue(MinValueProperty, value);
    }

    public static readonly BindableProperty MaxValueProperty =
        BindableProperty.Create(nameof(MaxValue), typeof(double), typeof(AngularGaugeView), 100d);

    public double MaxValue
    {
        get => (double)GetValue(MaxValueProperty);
        set => SetValue(MaxValueProperty, value);
    }

    public static readonly BindableProperty InitialRotationProperty =
        BindableProperty.Create(nameof(InitialRotation), typeof(double), typeof(AngularGaugeView), -225d);

    public double InitialRotation
    {
        get => (double)GetValue(InitialRotationProperty);
        set => SetValue(InitialRotationProperty, value);
    }

    public static readonly BindableProperty MaxAngleProperty =
        BindableProperty.Create(nameof(MaxAngle), typeof(double), typeof(AngularGaugeView), 270d);

    public double MaxAngle
    {
        get => (double)GetValue(MaxAngleProperty);
        set => SetValue(MaxAngleProperty, value);
    }

    public static new readonly BindableProperty WidthRequestProperty =
        BindableProperty.Create(nameof(WidthRequest), typeof(double), typeof(AngularGaugeView), 250d);

    public new double WidthRequest
    {
        get => (double)GetValue(WidthRequestProperty);
        set => SetValue(WidthRequestProperty, value);
    }

    public static new readonly BindableProperty HeightRequestProperty =
        BindableProperty.Create(nameof(HeightRequest), typeof(double), typeof(AngularGaugeView), 250d);

    public new double HeightRequest
    {
        get => (double)GetValue(HeightRequestProperty);
        set => SetValue(HeightRequestProperty, value);
    }
}