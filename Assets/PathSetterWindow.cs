using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class PathSetterWindow : FileExplorerWindow
{
    [SerializeField] private TextMeshProUGUI currentPathDisplay;
    public UnityEvent onSetSavePath;
    public UnityEvent onSetExportPath;
    private CardController _cardController;

    public static string ExportPath;
    
    private void Awake()
    {
        ExportPath = $"{Application.dataPath}/Card Stock/Cards/";
        _cardController = FindObjectOfType<CardController>();
    }

    public override void OpenWindow()
    {
        base.OpenWindow();
        currentPathDisplay.text = _currentPath;
    }

    public override void CreateFolder()
    {
        // Custom solution
        UnityAction action = RefreshList;
        DirectoryCreator.instance.PromptCreate(_currentPath,action);
    }
    
    protected override void GotoFolder(string dir)
    {
        base.GotoFolder(dir);
        currentPathDisplay.text = _currentPath;
    }

    public void SetSavePath()
    {
        if (!Directory.Exists(_currentPath)) return;
        _cardController.SetSavePath(_currentPath);
        onSetSavePath.Invoke();
        CloseWindow();
    }

    public void SetExportPath()
    {
        if (!Directory.Exists(_currentPath)) return;
        if(_cardController!=null)
            _cardController.SetExportPath(_currentPath);
        
        if (Directory.Exists(_currentPath))
            ExportPath = _currentPath;
        
        onSetExportPath.Invoke();
        CloseWindow();
    }
}
