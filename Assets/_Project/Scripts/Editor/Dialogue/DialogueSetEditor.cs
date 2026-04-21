using System;
using System.Collections.Generic;
using LibraryOfGamecraft.Dialogue;
using UnityEditor;
using UnityEngine;

namespace LibraryOfGamecraft.Editor.Dialogue
{
    [CustomEditor(typeof(DialogueSet))]
    public class DialogueSetEditor : UnityEditor.Editor
    {
        private static readonly Dictionary<string, Type> NodeTypes = new()
        {
            { "Message",  typeof(MessageNode)  },
            { "Choice",   typeof(ChoiceNode)   },
            { "Branch",   typeof(BranchNode)   },
            { "Event",    typeof(EventNode)    },
            { "Sequence", typeof(SequenceNode) },
            { "Jump",     typeof(JumpNode)     },
            { "End",      typeof(EndNode)      },
        };

        private static readonly Dictionary<DialogueNodeType, Color> NodeColors = new()
        {
            { DialogueNodeType.Message,  new Color(0.8f, 0.95f, 0.8f)  },
            { DialogueNodeType.Choice,   new Color(0.8f, 0.85f, 1.0f)  },
            { DialogueNodeType.Branch,   new Color(1.0f, 0.95f, 0.7f)  },
            { DialogueNodeType.Event,    new Color(1.0f, 0.8f,  0.8f)  },
            { DialogueNodeType.Sequence, new Color(0.9f, 0.8f,  1.0f)  },
            { DialogueNodeType.Jump,     new Color(0.8f, 1.0f,  0.95f) },
            { DialogueNodeType.End,      new Color(0.85f, 0.85f, 0.85f)},
        };

        private int _deleteIndex = -1;
        private int _moveFromIndex = -1;
        private int _moveToIndex = -1;
        private bool _showNodeList = true;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // グラフエディタ起動ボタン
            var prevColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.4f, 0.7f, 1.0f);
            if (GUILayout.Button("▶  グラフエディタで開く", GUILayout.Height(28)))
                DialogueGraphEditorWindow.Open((DialogueSet)target);
            GUI.backgroundColor = prevColor;

            EditorGUILayout.Space(4);
            DrawDefaultInspectorExcluding("Nodes");
            EditorGUILayout.Space(4);

            _showNodeList = EditorGUILayout.Foldout(_showNodeList, $"Nodes ({GetNodeCount()})", true, EditorStyles.foldoutHeader);
            if (_showNodeList)
            {
                DrawNodeList();
                EditorGUILayout.Space(4);
                DrawAddNodeButtons();
            }

            serializedObject.ApplyModifiedProperties();

            // 削除・移動は ApplyModifiedProperties 後に実行
            HandleDeferredOperations();
        }

        private int GetNodeCount()
        {
            var set = (DialogueSet)target;
            return set.Nodes?.Count ?? 0;
        }

        private void DrawNodeList()
        {
            var set = (DialogueSet)target;
            if (set.Nodes == null || set.Nodes.Count == 0)
            {
                EditorGUILayout.HelpBox("ノードがありません。下のボタンで追加してください。", MessageType.Info);
                return;
            }

            for (int i = 0; i < set.Nodes.Count; i++)
            {
                var node = set.Nodes[i];
                if (node == null)
                {
                    DrawNullNode(i);
                    continue;
                }

                DrawNode(i, node);
                EditorGUILayout.Space(2);
            }
        }

        private void DrawNode(int index, DialogueNodeBase node)
        {
            var bgColor = NodeColors.TryGetValue(node.NodeType, out var c) ? c : Color.white;

            var boxStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(8, 8, 6, 6)
            };

            var prevColor = GUI.backgroundColor;
            GUI.backgroundColor = bgColor;

            EditorGUILayout.BeginVertical(boxStyle);
            GUI.backgroundColor = prevColor;

            // ヘッダー行
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"[{index}] {node.NodeType}  ID: {node.NodeId}", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("↑", GUILayout.Width(24)) && index > 0)
            {
                _moveFromIndex = index;
                _moveToIndex = index - 1;
            }
            if (GUILayout.Button("↓", GUILayout.Width(24)) && index < GetNodeCount() - 1)
            {
                _moveFromIndex = index;
                _moveToIndex = index + 1;
            }
            if (GUILayout.Button("✕", GUILayout.Width(24)))
                _deleteIndex = index;

            EditorGUILayout.EndHorizontal();

            // ノードの中身をSerializedPropertyで描画
            var nodesProp = serializedObject.FindProperty("Nodes");
            var nodeProp = nodesProp.GetArrayElementAtIndex(index);
            DrawNodeProperties(nodeProp, node);

            EditorGUILayout.EndVertical();
        }

        private void DrawNodeProperties(SerializedProperty nodeProp, DialogueNodeBase node)
        {
            EditorGUI.indentLevel++;

            // 共通フィールド
            DrawField(nodeProp, "NodeId");
            DrawField(nodeProp, "Comment");
            DrawField(nodeProp, "NextNodeId");
            DrawField(nodeProp, "SkipPolicy");
            DrawField(nodeProp, "LogPolicy");
            DrawFoldoutArray(nodeProp, "Conditions");
            DrawFoldoutArray(nodeProp, "PreEvents");
            DrawFoldoutArray(nodeProp, "PostEvents");

            // ノード種別固有フィールド
            switch (node.NodeType)
            {
                case DialogueNodeType.Message:
                    DrawField(nodeProp, "SpeakerDisplayName");
                    DrawField(nodeProp, "SpeakerId");
                    DrawTextField(nodeProp, "Text");
                    DrawField(nodeProp, "VoiceId");
                    DrawField(nodeProp, "TextSpeedOverride");
                    DrawField(nodeProp, "AutoAdvanceEnabled");
                    DrawField(nodeProp, "AutoAdvanceDelay");
                    break;

                case DialogueNodeType.Choice:
                    DrawTextField(nodeProp, "PromptText");
                    DrawFoldoutArray(nodeProp, "Choices");
                    break;

                case DialogueNodeType.Branch:
                    DrawFoldoutArray(nodeProp, "Branches");
                    DrawField(nodeProp, "DefaultNextNodeId");
                    break;

                case DialogueNodeType.Event:
                    DrawFoldoutArray(nodeProp, "Events");
                    DrawField(nodeProp, "WaitMode");
                    break;

                case DialogueNodeType.Sequence:
                    DrawField(nodeProp, "SequenceId");
                    DrawField(nodeProp, "WaitForCompletion");
                    DrawField(nodeProp, "AllowSkip");
                    break;

                case DialogueNodeType.Jump:
                    DrawField(nodeProp, "TargetSetId");
                    DrawField(nodeProp, "TargetNodeId");
                    break;

                case DialogueNodeType.End:
                    DrawField(nodeProp, "EndReason");
                    DrawFoldoutArray(nodeProp, "EndEvents");
                    break;
            }

            EditorGUI.indentLevel--;
        }

        private void DrawNullNode(int index)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.HelpBox($"[{index}] null ノード", MessageType.Warning);
            if (GUILayout.Button("削除", GUILayout.Width(48)))
                _deleteIndex = index;
            EditorGUILayout.EndHorizontal();
        }

        private void DrawAddNodeButtons()
        {
            EditorGUILayout.LabelField("ノードを追加:", EditorStyles.miniLabel);
            EditorGUILayout.BeginHorizontal();
            foreach (var kv in NodeTypes)
            {
                if (GUILayout.Button(kv.Key, GUILayout.Height(22)))
                    AddNode(kv.Value);
            }
            EditorGUILayout.EndHorizontal();
        }

        private void AddNode(Type nodeType)
        {
            var set = (DialogueSet)target;
            Undo.RecordObject(set, $"Add {nodeType.Name}");

            var node = (DialogueNodeBase)Activator.CreateInstance(nodeType);
            node.NodeType = GetNodeTypeEnum(nodeType);
            node.NodeId = $"Node_{set.Nodes.Count + 1:000}";

            set.Nodes.Add(node);
            EditorUtility.SetDirty(set);
        }

        private void HandleDeferredOperations()
        {
            var set = (DialogueSet)target;

            if (_deleteIndex >= 0)
            {
                Undo.RecordObject(set, "Delete DialogueNode");
                set.Nodes.RemoveAt(_deleteIndex);
                EditorUtility.SetDirty(set);
                _deleteIndex = -1;
            }

            if (_moveFromIndex >= 0)
            {
                Undo.RecordObject(set, "Move DialogueNode");
                var node = set.Nodes[_moveFromIndex];
                set.Nodes.RemoveAt(_moveFromIndex);
                set.Nodes.Insert(_moveToIndex, node);
                EditorUtility.SetDirty(set);
                _moveFromIndex = -1;
                _moveToIndex = -1;
            }
        }

        // ─────────────────────────────────────
        // ユーティリティ
        // ─────────────────────────────────────

        private static void DrawField(SerializedProperty parent, string fieldName)
        {
            var prop = parent.FindPropertyRelative(fieldName);
            if (prop != null) EditorGUILayout.PropertyField(prop, true);
        }

        private static void DrawTextField(SerializedProperty parent, string fieldName)
        {
            var prop = parent.FindPropertyRelative(fieldName);
            if (prop == null) return;
            EditorGUILayout.LabelField(fieldName, EditorStyles.miniLabel);
            prop.stringValue = EditorGUILayout.TextArea(prop.stringValue, GUILayout.MinHeight(40));
        }

        private static void DrawFoldoutArray(SerializedProperty parent, string fieldName)
        {
            var prop = parent.FindPropertyRelative(fieldName);
            if (prop != null && prop.isArray)
                EditorGUILayout.PropertyField(prop, new GUIContent(fieldName), true);
        }

        private void DrawDefaultInspectorExcluding(string excludeField)
        {
            var prop = serializedObject.GetIterator();
            prop.NextVisible(true);
            while (prop.NextVisible(false))
            {
                if (prop.name == excludeField) continue;
                EditorGUILayout.PropertyField(prop, true);
            }
        }

        private static DialogueNodeType GetNodeTypeEnum(Type t)
        {
            if (t == typeof(MessageNode))  return DialogueNodeType.Message;
            if (t == typeof(ChoiceNode))   return DialogueNodeType.Choice;
            if (t == typeof(BranchNode))   return DialogueNodeType.Branch;
            if (t == typeof(EventNode))    return DialogueNodeType.Event;
            if (t == typeof(SequenceNode)) return DialogueNodeType.Sequence;
            if (t == typeof(JumpNode))     return DialogueNodeType.Jump;
            return DialogueNodeType.End;
        }
    }
}
