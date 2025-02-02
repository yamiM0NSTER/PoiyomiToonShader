﻿// Material/Shader Inspector for Unity 2017/2018
// Copyright (C) 2019 Thryrallo

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Thry
{
    public class Settings : EditorWindow
    {
        //this is dope: this.ShowNotification(new GUIContent(s));

        // Add menu named "My Window" to the Window menu
        [MenuItem("Thry/Settings")]
        static void Init()
        {
            // Get existing open window or if none, make a new one:
            Settings window = (Settings)EditorWindow.GetWindow(typeof(Settings));
            window.Show();
        }

        public static void firstTimePopup()
        {
            Settings window = (Settings)EditorWindow.GetWindow(typeof(Settings));
            window.isFirstPopop = true;
            window.is_data_share_expanded = true;
            window.Show();
        }

        public static void updatedPopup(int compare)
        {
            Settings window = (Settings)EditorWindow.GetWindow(typeof(Settings));
            window.updatedVersion = compare;
            window.Show();
        }

        public new void Show()
        {
            base.Show();
        }

        public ModuleSettings[] moduleSettings;

        private bool isFirstPopop = false;
        private int updatedVersion = 0;

        private bool is_init = false;

        public static bool is_changing_vrc_sdk = false;
        
        public static ButtonData thry_message = null;

        //------------------Message Calls-------------------------

        public void OnDestroy()
        {
            if ((isFirstPopop|| updatedVersion!=0) && Config.Get().share_user_data)
                WebHelper.SendAnalytics();
            if (!EditorPrefs.GetBool("thry_has_counted_user", false))
            {
                WebHelper.DownloadStringASync(URL.COUNT_USER, delegate (string s)
                {
                    if (s == "true")
                        EditorPrefs.SetBool("thry_has_counted_user", true);
                });
            }
            
            string projectPrefix = PlayerSettings.companyName + "." +PlayerSettings.productName;
            if (!EditorPrefs.GetBool(projectPrefix+"_thry_has_counted_project", false))
            {
                WebHelper.DownloadStringASync(URL.COUNT_PROJECT, delegate (string s)
                {
                    if (s == "true")
                        EditorPrefs.SetBool(projectPrefix+"_thry_has_counted_project", true);
                });
            }
        }

        //---------------------Stuff checkers and fixers-------------------

        //checks if slected shaders is using editor
        private void OnSelectionChange()
        {
            string[] selectedAssets = Selection.assetGUIDs;
            if (selectedAssets.Length == 1)
            {
                UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(AssetDatabase.GUIDToAssetPath(selectedAssets[0]));
                if (obj.GetType() == typeof(Shader))
                {
                    Shader shader = (Shader)obj;
                    Material m = new Material(shader);
                    if (m.HasProperty(Shader.PropertyToID(ThryEditor.PROPERTY_NAME_USING_THRY_EDITOR)))
                    {
                        Mediator.SetActiveShader(shader,m);
                    }
                }
            }
            this.Repaint();
        }

        public void Awake()
        {
            InitVariables();
        }

        private void InitVariables()
        {
            is_changing_vrc_sdk = (FileHelper.LoadValueFromFile("delete_vrc_sdk", PATH.AFTER_COMPILE_DATA) == "true") || (FileHelper.LoadValueFromFile("update_vrc_sdk", PATH.AFTER_COMPILE_DATA) == "true");

            CheckVRCSDK();

            List<Type> subclasses = typeof(ModuleSettings).Assembly.GetTypes().Where(type => type.IsSubclassOf(typeof(ModuleSettings))).ToList<Type>();
            moduleSettings = new ModuleSettings[subclasses.Count];
            int i = 0;
            foreach(Type classtype in subclasses)
            {
                moduleSettings[i++] = (ModuleSettings)Activator.CreateInstance(classtype);
            }

            is_init = true;

            if (thry_message == null)
                WebHelper.DownloadStringASync(Thry.URL.SETTINGS_MESSAGE_URL, delegate (string s) { thry_message = Parser.ParseToObject<ButtonData>(s); });
        }

        private static void CheckVRCSDK()
        {
            if (!Settings.is_changing_vrc_sdk)
                UnityHelper.SetDefineSymbol(DEFINE_SYMBOLS.VRC_SDK_INSTALLED, VRCInterface.Get().sdk_is_installed);
        }

        //------------------Helpers----------------------------

        public static Settings getInstance()
        {
            Settings instance = (Settings)UnityHelper.FindEditorWindow(typeof(Settings));
            if (instance == null) instance = ScriptableObject.CreateInstance<Settings>();
            return instance;
        }

        //------------------Main GUI
        void OnGUI()
        {
            if (!is_init || moduleSettings==null) InitVariables();
            GUILayout.Label("ThryEditor v" + Config.Get().verion);

            GUINotification();
            drawLine();
            GUIMessage();
            GUIVRC();
            LocaleDropdown();
            GUIEditor();
            drawLine();
            GUIExtras();
            drawLine();
            GUIShareData();
            drawLine();
            foreach(ModuleSettings s in moduleSettings)
            {
                s.Draw();
                drawLine();
            }
            GUIModulesInstalation();
        }

        //--------------------------GUI Helpers-----------------------------

        private static void drawLine()
        {
            Rect rect = EditorGUILayout.GetControlRect(false, 1);
            rect.height = 1;
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 1));
        }

        private void GUINotification()
        {
            if (isFirstPopop)
                GUILayout.Label(" " + Locale.editor.Get("first_install_message"), Styles.Get().greenStyle);
            else if (updatedVersion == -1)
                GUILayout.Label(" " + Locale.editor.Get("update_message"), Styles.Get().greenStyle);
            else if (updatedVersion == 1)
                GUILayout.Label(" " + Locale.editor.Get("downgrade_message"), Styles.Get().yellowStyle);
        }

        private void GUIMessage()
        {
            if(thry_message!=null && thry_message.text.Length > 0)
            {
                GUIStyle style = new GUIStyle();
                style.richText = true;
                style.margin = new RectOffset(7, 0, 0, 0);
                style.wordWrap = true;
                GUILayout.Label(new GUIContent(thry_message.text,thry_message.hover), style);
                Rect r = GUILayoutUtility.GetLastRect();
                if (Event.current.type == EventType.MouseDown && r.Contains(Event.current.mousePosition))
                    thry_message.action.Perform();
                drawLine();
            }
        }

        private void GUIVRC()
        {
            if (VRCInterface.Get().sdk_is_installed)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("VRC Sdk "+Locale.editor.Get("version")+": " + VRCInterface.Get().installed_sdk_version + (VRCInterface.Get().sdk_is_up_to_date ? " ("+ Locale.editor.Get("newest")+ " "+Locale.editor.Get("version")+")" : ""));
                RemoveVRCSDKButton();
                GUILayout.EndHorizontal();
                if (!VRCInterface.Get().sdk_is_up_to_date)
                {
                    GUILayout.Label(Locale.editor.Get("newest") +" VRC SDK "+ Locale.editor.Get("version") +": " + VRCInterface.Get().newest_sdk_version);
                    UpdateVRCSDKButton();
                }
                if (VRCInterface.Get().user_logged_in)
                {
                    GUILayout.Label("VRChat "+ Locale.editor.Get("user")+": " + EditorPrefs.GetString("sdk#username"));
                }
            }
            else
            {
                InstallVRCSDKButton();
            }
            drawLine();
        }

        private void InstallVRCSDKButton()
        {
            EditorGUI.BeginDisabledGroup(is_changing_vrc_sdk);
            if (GUILayout.Button(Locale.editor.Get("button_install_vrc_sdk")))
            {
                is_changing_vrc_sdk = true;
                VRCInterface.DownloadAndInstallVRCSDK();
            }
            EditorGUI.EndDisabledGroup();
        }

        private void RemoveVRCSDKButton()
        {
            EditorGUI.BeginDisabledGroup(is_changing_vrc_sdk);
            if (GUILayout.Button(Locale.editor.Get("button_remove_vrc_sdk"), GUILayout.ExpandWidth(false)))
            {
                is_changing_vrc_sdk = true;
                VRCInterface.Get().RemoveVRCSDK(true);
            }
            EditorGUI.EndDisabledGroup();
        }

        private void UpdateVRCSDKButton()
        {
            EditorGUI.BeginDisabledGroup(is_changing_vrc_sdk);
            if (GUILayout.Button(Locale.editor.Get("button_update_vrc_sdk")))
            {
                is_changing_vrc_sdk = true;
                VRCInterface.Get().UpdateVRCSDK();
            }
            EditorGUI.EndDisabledGroup();
        }

        bool is_editor_expanded = true;
        private void GUIEditor()
        {
            is_editor_expanded = Foldout(Locale.editor.Get("header_editor"), is_editor_expanded);
            if (is_editor_expanded)
            {
                EditorGUI.indentLevel += 2;
                Dropdown("default_texture_type");
                Toggle("showRenderQueue");
                if (Config.Get().showRenderQueue)
                    Toggle("renderQueueShaders");
                GUIGradients();
                EditorGUI.indentLevel -= 2;
            }
        }

        private static void GUIGradients()
        {
            GUILayout.BeginHorizontal(GUILayout.ExpandWidth(false));
            Text("gradient_name", false);
            string gradient_name = Config.Get().gradient_name;
            if (gradient_name.Contains("<hash>"))
                GUILayout.Label(Locale.editor.Get("gradient_good_naming"), Styles.Get().greenStyle, GUILayout.ExpandWidth(false));
            else if (gradient_name.Contains("<material>"))
                if (gradient_name.Contains("<prop>"))
                    GUILayout.Label(Locale.editor.Get("gradient_good_naming"), Styles.Get().greenStyle, GUILayout.ExpandWidth(false));
                else
                    GUILayout.Label(Locale.editor.Get("gradient_add_hash_or_prop"), Styles.Get().yellowStyle, GUILayout.ExpandWidth(false));
            else if (gradient_name.Contains("<prop>"))
                GUILayout.Label(Locale.editor.Get("gradient_add_material"), Styles.Get().yellowStyle, GUILayout.ExpandWidth(false));
            else
                GUILayout.Label(Locale.editor.Get("gradient_add_material_or_prop"), Styles.Get().redStyle, GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();
        }

        bool is_extras_expanded = false;
        private void GUIExtras()
        {
            is_extras_expanded = Foldout(Locale.editor.Get("header_extras"), is_extras_expanded);
            if (is_extras_expanded)
            {
                EditorGUI.indentLevel += 2;
                Toggle("showImportPopup");
                EditorGUI.indentLevel -= 2;
            }
        }

        bool is_data_share_expanded = false;
        private void GUIShareData()
        {
            is_data_share_expanded = Foldout(Locale.editor.Get("header_user_data_collection"), is_data_share_expanded);
            if (is_data_share_expanded)
            {
                EditorGUI.indentLevel += 2;
                Toggle("share_user_data", EditorStyles.boldLabel);
                EditorGUILayout.LabelField(Locale.editor.Get("share_data_info_message"));
                if (Config.Get().share_user_data)
                {
                    Toggle("share_installed_unity_version");
                    Toggle("share_installed_editor_version");
                    Toggle("share_used_shaders");
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(EditorGUI.indentLevel * 15);
                    if (GUILayout.Button(Locale.editor.Get("button_get_my_data"), GUILayout.ExpandWidth(false)))
                    {
                        WebHelper2.DownloadStringASync(URL.DATA_SHARE_GET_MY_DATA+"?hash="+WebHelper.GetMacAddress().GetHashCode(), delegate(string s){
                            TextPopup popup = ScriptableObject.CreateInstance<TextPopup>();
                            popup.position = new Rect(Screen.width / 2, Screen.height / 2, 512, 480);
                            popup.titleContent = new GUIContent(Locale.editor.Get("your_data"));
                            popup.text = s;
                            popup.ShowUtility();
                        });
                    }
                    GUILayout.EndHorizontal();
                }
                EditorGUI.indentLevel -= 2;
            }
        }

        private class TextPopup : EditorWindow
        {
            public string text = "";
            private Vector2 scroll;
            void OnGUI()
            {
                EditorGUILayout.SelectableLabel(Locale.editor.Get("my_data_header"), EditorStyles.boldLabel);
                Rect last = GUILayoutUtility.GetLastRect();
                
                Rect data_rect = new Rect(0, last.height, Screen.width, Screen.height - last.height);
                scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.Width(data_rect.width), GUILayout.Height(data_rect.height));
                GUILayout.TextArea(text);
                EditorGUILayout.EndScrollView();
            }
        }

        private void GUIModulesInstalation()
        {
            if (ModuleHandler.GetModules() == null)
                return;
            if (ModuleHandler.GetModules().Count > 0)
                GUILayout.Label(Locale.editor.Get("header_modules"), EditorStyles.boldLabel);
            bool disabled = false;
            foreach (ModuleHeader module in ModuleHandler.GetModules())
                if (module.is_being_installed_or_removed)
                    disabled = true;
            EditorGUI.BeginDisabledGroup(disabled);
            foreach (ModuleHeader module in ModuleHandler.GetModules())
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUI.BeginDisabledGroup(!module.available_requirement_fullfilled);
                EditorGUI.BeginChangeCheck();
                bool is_installed = Helper.ClassExists(module.available_module.classname);
                bool update_available = is_installed;
                if (module.installed_module != null)
                    update_available = Helper.compareVersions(module.installed_module.version, module.available_module.version) == 1;
                string displayName = module.available_module.name;
                if (module.installed_module != null)
                    displayName += " v" + module.installed_module.version;

                bool install = GUILayout.Toggle(is_installed, new GUIContent(displayName, module.available_module.description), GUILayout.ExpandWidth(false));
                if (EditorGUI.EndChangeCheck())
                    ModuleHandler.InstallRemoveModule(module,install);
                if(update_available)
                    if (GUILayout.Button("update to v"+module.available_module.version, GUILayout.ExpandWidth(false)))
                        ModuleHandler.UpdateModule(module);
                EditorGUI.EndDisabledGroup();
                if (module.available_module.requirement != null && (update_available || !is_installed))
                {
                    if(module.available_requirement_fullfilled)
                        GUILayout.Label(Locale.editor.Get("requirements") +": " + module.available_module.requirement.ToString(), Styles.Get().greenStyle);
                    else
                        GUILayout.Label(Locale.editor.Get("requirements") + ": " + module.available_module.requirement.ToString(), Styles.Get().redStyle);
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUI.EndDisabledGroup();
        }

        private static void Text(string configField, bool createHorizontal = true)
        {
            Text(configField, Locale.editor.Get(configField), Locale.editor.Get(configField + "_tooltip"), createHorizontal);
        }

        private static void Text(string configField, string[] content, bool createHorizontal=true)
        {
            Text(configField, content[0], content[1], createHorizontal);
        }

        private static void Text(string configField, string text, string tooltip, bool createHorizontal)
        {
            Config config = Config.Get();
            System.Reflection.FieldInfo field = typeof(Config).GetField(configField);
            if (field != null)
            {
                string value = (string)field.GetValue(config);
                if (createHorizontal)
                    GUILayout.BeginHorizontal(GUILayout.ExpandWidth(false));
                GUILayout.Space(57);
                GUILayout.Label(new GUIContent(text, tooltip), GUILayout.ExpandWidth(false));
                EditorGUI.BeginChangeCheck();
                value = EditorGUILayout.DelayedTextField("", value, GUILayout.ExpandWidth(false));
                if (EditorGUI.EndChangeCheck())
                {
                    field.SetValue(config, value);
                    config.save();
                }
                if (createHorizontal)
                    GUILayout.EndHorizontal();
            }
        }

        private static void Toggle(string configField, GUIStyle label_style = null)
        {
            Toggle(configField, Locale.editor.Get(configField), Locale.editor.Get(configField + "_tooltip"), label_style);
        }

        private static void Toggle(string configField, string[] content, GUIStyle label_style = null)
        {
            Toggle(configField, content[0], content[1], label_style);
        }

        private static void Toggle(string configField, string label, string hover, GUIStyle label_style = null)
        {
            Config config = Config.Get();
            System.Reflection.FieldInfo field = typeof(Config).GetField(configField);
            if (field != null)
            {
                bool value = (bool)field.GetValue(config);
                if (Toggle(value, label, hover, label_style) != value)
                {
                    field.SetValue(config, !value);
                    config.save();
                    ThryEditor.repaint();
                }
            }
        }

        private static void Dropdown(string configField)
        {
            Dropdown(configField, Locale.editor.Get(configField),Locale.editor.Get(configField+"_tooltip"));
        }

        private static void Dropdown(string configField, string[] content)
        {
            Dropdown(configField, content[0], content[1]);
        }

        private static void Dropdown(string configField, string label, string hover, GUIStyle label_style = null)
        {
            Config config = Config.Get();
            System.Reflection.FieldInfo field = typeof(Config).GetField(configField);
            if (field != null)
            {
                Enum value = (Enum)field.GetValue(config);
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(57);
                GUILayout.Label(new GUIContent(label, hover), GUILayout.ExpandWidth(false));
                value = EditorGUILayout.EnumPopup(value,GUILayout.ExpandWidth(false));
                EditorGUILayout.EndHorizontal();
                if(EditorGUI.EndChangeCheck())
                {
                    field.SetValue(config, value);
                    config.save();
                    ThryEditor.repaint();
                }
            }
        }

        private static void LocaleDropdown()
        {
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(new GUIContent(Locale.editor.Get("locale"), Locale.editor.Get("locale_tooltip")), GUILayout.ExpandWidth(false));
            Locale.editor.selected_locale_index = EditorGUILayout.Popup(Locale.editor.selected_locale_index, Locale.editor.available_locales, GUILayout.ExpandWidth(false));
            if(Locale.editor.Get("translator").Length>0)
                GUILayout.Label(Locale.editor.Get("translation") +": "+Locale.editor.Get("translator"), GUILayout.ExpandWidth(false));
            EditorGUILayout.EndHorizontal();
            if(EditorGUI.EndChangeCheck())
            {
                Config.Get().locale = Locale.editor.available_locales[Locale.editor.selected_locale_index];
                Config.Get().save();
                ThryEditor.reload();
                ThryEditor.repaint();
            }
        }

        private static bool Toggle(bool val, string text, GUIStyle label_style = null)
        {
            return Toggle(val, text, "",label_style);
        }

        private static bool Toggle(bool val, string text, string tooltip, GUIStyle label_style=null)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(35);
            val = GUILayout.Toggle(val, new GUIContent("", tooltip), GUILayout.ExpandWidth(false));
            if(label_style==null)
                GUILayout.Label(new GUIContent(text, tooltip));
            else
                GUILayout.Label(new GUIContent(text, tooltip),label_style);
            GUILayout.EndHorizontal();
            return val;
        }

        private static bool Foldout(string text, bool expanded)
        {
            return Foldout(new GUIContent(text), expanded);
        }

        private static bool Foldout(GUIContent content, bool expanded)
        {
            var rect = GUILayoutUtility.GetRect(16f + 20f, 22f, Styles.Get().dropDownHeader);
            GUI.Box(rect, content, Styles.Get().dropDownHeader);
            var toggleRect = new Rect(rect.x + 4f, rect.y + 2f, 13f, 13f);
            Event e = Event.current;
            if (e.type == EventType.Repaint)
                EditorStyles.foldout.Draw(toggleRect, false, false, expanded, false);
            if (e.type == EventType.MouseDown && rect.Contains(e.mousePosition) && !e.alt)
            {
                expanded = !expanded;
                e.Use();
            }
            return expanded;
        }
    }
}