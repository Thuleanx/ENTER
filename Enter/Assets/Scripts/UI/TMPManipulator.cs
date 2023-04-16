using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace Enter {
    public class TMPManipulator: MonoBehaviour
    {
        protected TMP_Text textMesh;
        protected Mesh mesh;
        protected Vector3[] vertices;
        protected RectTransform rectTransform;

        public virtual void Awake()
        {
            textMesh = GetComponentInChildren<TMP_Text>();
            rectTransform = GetComponentInChildren<RectTransform>();
        }

        void OnEnable() {
            mesh = textMesh.mesh;
            vertices = mesh.vertices;
        }

        public virtual void Update()
        {
            textMesh.ForceMeshUpdate();
            mesh = textMesh.mesh;
            vertices = mesh.vertices;
        }

        public void LateUpdate() {
            mesh.vertices = vertices;
            textMesh.canvasRenderer.SetMesh(mesh);
        }

        public virtual void SetText(string text) { textMesh.text = text; }
    }
}
