using System;
using Tiles;
using UnityEngine;

public class UIManager : MonoBehaviour {
    Canvas _canvas;
    WangTileGenerator _generator;
    GameObject _tileCollection;
    Collider2D[] _colliders;

    void Start() {
        _canvas                =  GetComponent<Canvas>();
        _generator             =  FindFirstObjectByType<WangTileGenerator>();
        _generator.OnMapUpdate += UpdateTileCollection;
        
        // assume it's available
        UpdateTileCollection();
        foreach (Collider2D c in _colliders) {
            c.enabled = !_canvas.enabled;
        }
    }

    void Update() {
        
        if (Input.GetKeyDown(KeyCode.Escape)) {
            _canvas.enabled = !_canvas.enabled;
            foreach (Collider2D c in _colliders) {
                c.enabled = !_canvas.enabled;
            }
        }
    }
    void UpdateTileCollection() {
        _tileCollection = GameObject.FindWithTag("TileContainer");
        _colliders = _tileCollection?.GetComponentsInChildren<Collider2D>();
    }

    public void SaveImage() {
        throw new NotImplementedException("Screenshot functionality is not supported yet!");
    }
}
