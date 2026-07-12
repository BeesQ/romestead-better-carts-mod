using System;
using System.IO;
using System.Reflection;
using BepInEx.Configuration;
using BepInEx.Logging;

namespace BetterCarts;

internal static class MsmIntegration {
    private static ManualLogSource _log;
    private static ConfigFile _config;
    private static bool _registered;

    internal static void Init(ManualLogSource log, ConfigFile config) {
        _log = log;
        _config = config;
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies()) {
            if (TryRegisterFrom(assembly)) {
                return;
            }
        }
        _log.LogInfo("Mod Settings Menu not detected at load, using plain config (will register if it loads later).");
        // Without a [BepInDependency] attribute the chainloader may load Mod Settings Menu after this plugin, so registration waits for its assembly to appear
        AppDomain.CurrentDomain.AssemblyLoad += OnAssemblyLoad;
    }

    private static void OnAssemblyLoad(object sender, AssemblyLoadEventArgs args) {
        if (!_registered && TryRegisterFrom(args.LoadedAssembly)) {
            AppDomain.CurrentDomain.AssemblyLoad -= OnAssemblyLoad;
        }
    }

    private static bool TryRegisterFrom(Assembly assembly) {
        if (assembly == null || assembly.IsDynamic) {
            return false;
        }
        Type registryType = null;
        Type optionsType = null;
        Type[] types;
        try {
            types = assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex) {
            types = ex.Types;
        }
        catch {
            return false;
        }
        if (types == null) {
            return false;
        }
        foreach (Type type in types) {
            if (type == null) {
                continue;
            }
            if (type.Name == "ModSettingsRegistry") {
                registryType = type;
            }
            else if (type.Name == "ModSettingsModOptions") {
                optionsType = type;
            }
        }
        if (registryType == null || optionsType == null) {
            return false;
        }
        try {
            Register(registryType, optionsType);
            _log.LogInfo("Mod Settings Menu detected, mod metadata registered.");
        }
        catch (Exception ex) {
            _log.LogWarning("Mod Settings Menu registration failed: " + ex.Message);
        }
        _registered = true;
        return true;
    }

    private static void Register(Type registryType, Type optionsType) {
        MethodInfo register = null;
        foreach (MethodInfo method in registryType.GetMethods(BindingFlags.Public | BindingFlags.Static)) {
            if (method.Name != "Register") {
                continue;
            }
            ParameterInfo[] parameters = method.GetParameters();
            if (parameters.Length == 4 && parameters[3].ParameterType == optionsType) {
                register = method;
                break;
            }
        }
        if (register == null) {
            throw new MissingMethodException("ModSettingsRegistry.Register(guid, name, config, ModSettingsModOptions) not found");
        }
        object options = Activator.CreateInstance(optionsType);
        SetMember(options, "Version", BetterCartsPlugin.PluginVersion);
        SetMember(options, "Author", "BeesQ");
        SetMember(options, "Description", "Better Carts makes hauling with Carts more pleasant with quality-of-life features, all configurable in-game");
        SetMember(options, "NexusModsId", 91);
        SetMember(options, "UpdateManifestUrl", "https://raw.githubusercontent.com/BeesQ/romestead-better-carts-mod/main/version.json");
        string iconPath = Path.Combine(Path.GetDirectoryName(typeof(BetterCartsPlugin).Assembly.Location) ?? string.Empty, "icon.png");
        if (File.Exists(iconPath)) {
            SetMember(options, "IconPath", iconPath);
        }
        register.Invoke(null, new object[] { BetterCartsPlugin.PluginGuid, BetterCartsPlugin.PluginName, _config, options });
    }

    private static void SetMember(object target, string name, object value) {
        Type type = target.GetType();
        PropertyInfo property = type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
        if (property != null && property.CanWrite) {
            property.SetValue(target, Coerce(value, property.PropertyType));
            return;
        }
        FieldInfo field = type.GetField(name, BindingFlags.Public | BindingFlags.Instance);
        if (field != null) {
            field.SetValue(target, Coerce(value, field.FieldType));
        }
    }

    private static object Coerce(object value, Type targetType) {
        if (value == null) {
            return null;
        }
        Type underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;
        if (underlying.IsInstanceOfType(value)) {
            return value;
        }
        try {
            return Convert.ChangeType(value, underlying);
        }
        catch {
            return value;
        }
    }
}
