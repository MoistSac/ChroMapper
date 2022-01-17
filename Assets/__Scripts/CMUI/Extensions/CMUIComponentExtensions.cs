using System;
using UnityEngine.Localization.Settings;

public static class CMUIComponentExtensions
{
    /// <summary>
    /// Specifies an accessor to get the initial value from.
    /// </summary>
    /// <typeparam name="TComponent">Inherited <see cref="CMUIComponent{T}"/>.</typeparam>
    /// <param name="initialValue">Initial value accessor</param>
    /// <returns>Itself, for use in chaining methods.</returns>
    public static TComponent WithInitialValue<TComponent, TValue>(this TComponent component, Func<TValue> initialValue)
        where TComponent : CMUIComponent<TValue>
    {
        component.SetValueAccessor(initialValue);
        return component;
    }

    /// <summary>
    /// Specifies a callback that is triggered when the component value has changed.
    /// </summary>
    /// <typeparam name="TComponent">Inherited <see cref="CMUIComponent{T}"/>.</typeparam>
    /// <param name="onValueChanged">Callback triggered on value changed</param>
    /// <returns>Itself, for use in chaining methods.</returns>
    public static TComponent OnChanged<TComponent, TValue>(this TComponent component, Action<TValue> onValueChanged)
        where TComponent : CMUIComponent<TValue>
    {
        component.SetOnValueChanged(onValueChanged);
        return component;
    }

    /// <summary>
    /// Specifies localized text for the label that accompanies the component.
    /// </summary>
    /// <typeparam name="TComponent">Inherited <see cref="CMUIComponentWithLabel{T}"/></typeparam>
    /// <param name="table">Table which holds the localized text</param>
    /// <param name="key">Key for the localized text</param>
    /// <param name="args">Additional arguments if string formatting is involved.</param>
    /// <returns>Itself, for use in chaining methods.</returns>
    public static TComponent WithLocalizedLabel<TComponent, TValue>(this TComponent component, 
        string table, string key, params object[] args)
        where TComponent : CMUIComponentWithLabel<TValue>
    {
        var str = LocalizationSettings.StringDatabase.GetLocalizedString(table, key, args);
        return component.WithUnlocalizedLabel<TComponent, TValue>(str);
    }

    /// <summary>
    /// Specifies unlocalized text for the label that accompanies the component.
    /// If <paramref name="labelText"/> is <c>null</c>, the label itself will be disabled.
    /// </summary>
    /// <remarks>
    /// For CM development, it is *HIGHLY* recommended to use <see cref="WithLocalizedText(string, string, object[])"/>,
    /// so any and all text can be localized to different languages.
    /// 
    /// For plugin developers, feel free to continue using this.
    /// </remarks>
    /// <typeparam name="TComponent">Inherited <see cref="CMUIComponentWithLabel{T}"/></typeparam>
    /// <param name="labelText">Text to display, if non-null.</param>
    /// <returns>Itself, for use in chaining methods.</returns>
    public static TComponent WithUnlocalizedLabel<TComponent, TValue>(this TComponent component, string labelText)
        where TComponent : CMUIComponentWithLabel<TValue>
    {
        component.SetLabelEnabled(string.IsNullOrWhiteSpace(labelText));
        component.SetLabelText(labelText ?? "null");
        return component;
    }

    /// <summary>
    /// Specifies that the component will have no label displayed.
    /// </summary>
    /// <typeparam name="TComponent">Inherited <see cref="CMUIComponentWithLabel{T}"/></typeparam>
    /// <returns>Itself, for use in chaining methods.</returns>
    public static TComponent WithNoLabelText<TComponent, TValue>(this TComponent component)
        where TComponent : CMUIComponentWithLabel<TValue>
    {
        component.SetLabelEnabled(false);
        return component;
    }
}