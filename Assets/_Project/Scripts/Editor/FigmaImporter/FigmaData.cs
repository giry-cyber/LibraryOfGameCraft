using System;
using System.Collections.Generic;

namespace FigmaImporter
{
    // Figma REST API / MCP から返ってくるノードデータの最小モデル

    [Serializable]
    public class FigmaFile
    {
        public string name;
        public FigmaNode document;
    }

    [Serializable]
    public class FigmaNode
    {
        public string id;
        public string name;
        public string type;          // DOCUMENT, CANVAS, FRAME, GROUP, RECTANGLE, TEXT, VECTOR, ELLIPSE, COMPONENT, INSTANCE …
        public bool visible = true;
        public FigmaAbsoluteBoundingBox absoluteBoundingBox;
        public FigmaColor backgroundColor;
        public List<FigmaFill> fills;
        public FigmaTypeStyle style;  // TEXT ノードのみ
        public string characters;     // TEXT ノードのみ
        public List<FigmaNode> children;
        public float opacity = 1f;
    }

    [Serializable]
    public class FigmaAbsoluteBoundingBox
    {
        public float x;
        public float y;
        public float width;
        public float height;
    }

    [Serializable]
    public class FigmaColor
    {
        public float r;
        public float g;
        public float b;
        public float a = 1f;
    }

    [Serializable]
    public class FigmaFill
    {
        public string type;   // SOLID, IMAGE, GRADIENT_LINEAR …
        public FigmaColor color;
        public float opacity = 1f;
        public bool visible = true;
    }

    [Serializable]
    public class FigmaTypeStyle
    {
        public string fontFamily;
        public float fontSize = 14f;
        public string textAlignHorizontal; // LEFT, CENTER, RIGHT
        public FigmaColor fills;            // ※ テキスト色は fills[0] に入っているが簡略化
    }
}
