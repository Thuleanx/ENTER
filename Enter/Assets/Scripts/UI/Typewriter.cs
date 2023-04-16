using UnityEngine;
using System.Collections;
using TMPro;
using NaughtyAttributes;

namespace Enter
{
    public class Typewriter : MonoBehaviour {
        protected TMP_Text textMesh;
        protected Mesh mesh;
        protected Vector3[] originalVertex;
        protected Vector3[] vertices;
        protected RectTransform rectTransform;

        public void Awake() {
            textMesh = GetComponentInChildren<TMP_Text>();
            rectTransform = GetComponentInChildren<RectTransform>();
        }

        void Start() {
            textMesh.ForceMeshUpdate();
            mesh = textMesh.mesh;
            originalVertex = mesh.vertices;
            vertices = mesh.vertices;
        }

        [Button("Erase")]
        public void EraseAllText() {
            for (int i = 0; i < textMesh.textInfo.characterCount; i++) {
                TMP_CharacterInfo c = textMesh.textInfo.characterInfo[i];
                if (c.character == ' ') continue;
                int index = c.vertexIndex;
                for (int j = 0; j < 4; j++)
                    vertices[index+j] = Vector2.zero;
            }
            mesh.vertices = vertices;
            textMesh.canvasRenderer.SetMesh(mesh);
        }

        public IEnumerator WaitForTypeWrite(float _charactersPerMinute) {
            for (int i = 0; i < textMesh.textInfo.characterCount; i++)
            {
                TMP_CharacterInfo c = textMesh.textInfo.characterInfo[i];
                if (c.character == ' ') continue;
                else {
                    /* textMesh.ForceMeshUpdate(); */
                    mesh = textMesh.mesh;
                    vertices = mesh.vertices;

                    int index = c.vertexIndex;
                    for (int j = 0; j < 4; j++) 
                        vertices[index+j] = originalVertex[index+j];

                    mesh.vertices = vertices;
                    textMesh.canvasRenderer.SetMesh(mesh);
                    yield return new WaitForSecondsRealtime(60.0f /_charactersPerMinute);
                }
            }
        }
    }
}
