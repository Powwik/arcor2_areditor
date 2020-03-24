using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Base;
using UnityEngine.EventSystems;

public class OutlineOnClick : Clickable {

    public Material ClickMaterial;

    public List<Renderer> Renderers = new List<Renderer>();
    private Dictionary<Renderer, List<Material>> materials = new Dictionary<Renderer, List<Material>>();
    
    private void Start() {
        materials.Clear();
        foreach (Renderer renderer in Renderers) {
            materials.Add(renderer, new List<Material>(renderer.materials));
        }
    }

    private void OnEnable() {
        InputHandler.Instance.OnBlindClick += OnBlindClick;
    }

    private void OnDisable() {
        if (InputHandler.Instance != null) {
            InputHandler.Instance.OnBlindClick -= OnBlindClick;
        }
    }

    public void InitRenderers(List<Renderer> renderers) {
        Renderers = renderers;
        materials.Clear();
        foreach (Renderer renderer in Renderers) {
            materials.Add(renderer, new List<Material>(renderer.materials));
        }
    }

    public override void OnClick(Click type) {
        // HANDLE MOUSE
        if (type == Click.MOUSE_RIGHT_BUTTON) {
            Select();
        }
        // HANDLE TOUCH
        else if (type == Click.TOUCH && !(ControlBoxManager.Instance.UseGizmoMove || ControlBoxManager.Instance.UseGizmoRotate)) {
            Select();
        }
    }

    private void AddMaterial(Material material) {
        foreach (Renderer renderer in Renderers) {
            if (!materials[renderer].Contains(material)) {
                materials[renderer].Add(material);
            }
        }
    }

    private void RemoveMaterial(Material material) {
        foreach (Renderer renderer in Renderers) {
            if (materials[renderer].Contains(material)) {
                materials[renderer].Remove(material);
            }
        }
    }

    private void OnBlindClick(object sender, EventBlindClickArgs e) {
        if (GameManager.Instance.SceneInteractable) {
            Scene.Instance.SetSelectedObject(null);
            RemoveMaterial(ClickMaterial);
            foreach (Renderer renderer in Renderers) {
                renderer.materials = materials[renderer].ToArray();
            }
        }
    }

    public void Deselect() {
        RemoveMaterial(ClickMaterial);
        foreach (Renderer renderer in Renderers) {
            renderer.materials = materials[renderer].ToArray();
        }
    }

    public void Select() {
        Scene.Instance.SetSelectedObject(gameObject);
        AddMaterial(ClickMaterial);
        foreach (Renderer renderer in Renderers) {
            renderer.materials = materials[renderer].ToArray();
        }
    }

    /// <summary>
    /// Removes mat1 (=old material) and Adds mat2 (=new material) to the materials array.
    /// </summary>
    /// <param name="mat1"></param>
    /// <param name="mat2"></param>
    public void SwapMaterials(Material mat1, Material mat2) {
        RemoveMaterial(mat1);
        AddMaterial(mat2);
    }
}