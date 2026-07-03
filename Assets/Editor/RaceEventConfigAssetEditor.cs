using System;
using ThreadRace.Infrastructure.Config;
using UnityEditor;
using UnityEngine;

namespace ThreadRace.Editor
{
    [CustomEditor(typeof(RaceEventConfigAsset))]
    public sealed class RaceEventConfigAssetEditor : UnityEditor.Editor
    {
        private SerializedProperty _saveSchemaVersion;
        private SerializedProperty _saveKey;
        private SerializedProperty _defaultSeed;
        private SerializedProperty _eventDurationSeconds;
        private SerializedProperty _countdownUpdateIntervalSeconds;
        private SerializedProperty _finishTarget;
        private SerializedProperty _rewardedPositionCount;
        private SerializedProperty _rewardTiers;
        private SerializedProperty _racers;

        private void OnEnable()
        {
            _saveSchemaVersion = serializedObject.FindProperty("_saveSchemaVersion");
            _saveKey = serializedObject.FindProperty("_saveKey");
            _defaultSeed = serializedObject.FindProperty("_defaultSeed");
            _eventDurationSeconds = serializedObject.FindProperty("_eventDurationSeconds");
            _countdownUpdateIntervalSeconds = serializedObject.FindProperty("_countdownUpdateIntervalSeconds");
            _finishTarget = serializedObject.FindProperty("_finishTarget");
            _rewardedPositionCount = serializedObject.FindProperty("_rewardedPositionCount");
            _rewardTiers = serializedObject.FindProperty("_rewardTiers");
            _racers = serializedObject.FindProperty("_racers");

            EditorApplication.delayCall += Repaint;
        }

        private void OnDisable()
        {
            EditorApplication.delayCall -= Repaint;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));
            }

            DrawRequiredProperty(_saveSchemaVersion, "_saveSchemaVersion");
            DrawRequiredProperty(_saveKey, "_saveKey");
            DrawRequiredProperty(_defaultSeed, "_defaultSeed");
            DrawRequiredProperty(_eventDurationSeconds, "_eventDurationSeconds");
            DrawRequiredProperty(_countdownUpdateIntervalSeconds, "_countdownUpdateIntervalSeconds");
            DrawRequiredProperty(_finishTarget, "_finishTarget");
            DrawRequiredProperty(_rewardedPositionCount, "_rewardedPositionCount");
            DrawRequiredProperty(_rewardTiers, "_rewardTiers", true);
            DrawRequiredProperty(_racers, "_racers", true);

            serializedObject.ApplyModifiedProperties();

            DrawValidationState();
        }

        private static void DrawRequiredProperty(
            SerializedProperty property,
            string propertyName,
            bool includeChildren = false)
        {
            if (property == null)
            {
                EditorGUILayout.HelpBox(
                    $"Missing serialized property '{propertyName}'. Reimport scripts or check field names.",
                    MessageType.Error);
                return;
            }

            EditorGUILayout.PropertyField(property, includeChildren);
        }

        private void DrawValidationState()
        {
            var config = (RaceEventConfigAsset)target;
            try
            {
                config.ToRuntimeSettings();
                EditorGUILayout.HelpBox("Runtime config is valid.", MessageType.Info);
            }
            catch (Exception exception)
            {
                EditorGUILayout.HelpBox(exception.Message, MessageType.Error);
            }
        }
    }
}
