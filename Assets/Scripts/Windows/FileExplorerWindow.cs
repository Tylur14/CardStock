using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

    public class FileExplorerWindow : Window
    {
        [Header("Display Settings")] 
    [SerializeField] protected TextMeshProUGUI windowNameText;
    [SerializeField] protected TextMeshProUGUI actionButtonText;
    [SerializeField] protected Sprite goBackIcon;
    [SerializeField] protected Sprite folderIcon;
    [SerializeField] protected GameObject fileObjectPrefab;
    [SerializeField] protected GameObject directoryObjectPrefab;
    [SerializeField] protected Transform listContainer;

    [Header("Targeting")]
    [SerializeField] private PathTarget.PathTargets rootPath;
    [SerializeField] protected bool showDirectories;
    [SerializeField] protected FileExplorerTarget[] fileTypeTargets;
    protected string _rootPath;
    public string CurrentPath => _currentPath;
    protected string _currentPath;
    public FileListObject TempObject => _tempObject;
    protected FileListObject _tempObject;
    protected FileListObject _tempDirectory;
    protected TMP_InputField _currentPathDisplay;

    [SerializeField] private FileSelectionProfile _lastProfile;

    private void ResetList()
    {
        _rootPath = _rootPath.Replace('/', '\\');
        if(_rootPath[^1] == '\\')
            _rootPath = _rootPath.Remove(_rootPath.Length-1);
        _currentPath = _currentPath.Replace('/', '\\');
        if(_currentPath[^1] == '\\')
            _currentPath = _currentPath.Remove(_currentPath.Length-1);
            
        _currentPath = string.IsNullOrEmpty(_currentPath) ? _rootPath : _currentPath;
        RefreshList();
    }

    public virtual void RefreshList()
    {
        if(_currentPathDisplay!=null)
            _currentPathDisplay.text = _currentPath;
        
        for (int i = 0; i < listContainer.childCount; i++)
        {
            Destroy(listContainer.GetChild(i).gameObject);
        }
        if (_currentPath != _rootPath)
        {
            var obj = Instantiate(directoryObjectPrefab, listContainer).GetComponent<FileListObject>();
            Action[] actions = new Action[1];
            actions[0] = delegate { GotoFolder(Directory.GetParent(_currentPath)?.ToString()); };
            obj.Setup("go back",actions,null,null,goBackIcon);
        }
        
        GetDirectories();
        GetFiles();
    }

    protected virtual void GetFiles()
    {
        var files = Directory.GetFiles(_currentPath).Where(o => !o.Contains(".meta")).ToList();
        foreach (var file in files)
        {
            foreach (var fileTypeTarget in fileTypeTargets)
            {
                if (file.ToLower().Contains(fileTypeTarget.fileType.ToLower()))
                {
                    var s = Path.GetFileNameWithoutExtension(file);
                    var obj = Instantiate(fileObjectPrefab, listContainer).GetComponent<FileListObject>();
                    obj.filePath = file;
                    _tempObject = obj;
                    SetupFile(obj,s,fileTypeTarget.fileIcon);
                }
            }
        }
    }

    protected virtual void SetupFile(FileListObject obj, string fileName, Sprite fileIcon)
    {
        obj.Setup(fileName, GetFileClickActions(),GetFileSelectActions(), null, fileIcon);
    }

    protected virtual Action[] GetFileSelectActions()
    {
        // example
        //Action[] actions = new Action[1];
        //actions[0] = delegate { Test("test"); };
        //return actions;
        return null;
    }
    
    protected virtual Action[] GetFileClickActions()
    {
        // example
        //Action[] actions = new Action[1];
        //actions[0] = delegate { Test("test"); };
        //return actions;
        return null;
    }

    protected virtual Action[] GetDirectorySelectActions()
    {
        return null;
    }
    
    protected virtual Action[] GetDirectoryClickActions()
    {
        var dir = _tempDirectory;
        Action[] actions = new Action[1];
        actions[0] = delegate { GotoFolder(dir.filePath); };
        return actions;
    }
    
    protected virtual void GetDirectories()
    {
        if (showDirectories)
        {
            var directories = Directory.GetDirectories(_currentPath);
            foreach (var folder in directories)
            {
                var obj = Instantiate(directoryObjectPrefab, listContainer).GetComponent<FileListObject>();
                _tempDirectory = obj;
                _tempDirectory.filePath = folder;
                Action[] actions = new Action[1];
                actions[0] = delegate { GotoFolder(folder); };
                obj.Setup(Path.GetFileNameWithoutExtension(folder),actions,GetDirectorySelectActions(),null,folderIcon);
            }
        }
    }
    
    public void OpenWindow(FileSelectionProfile profile)
    {
        if (_lastProfile != null)
        {
            if (_lastProfile == profile)
            {
                OpenWindow();
                ResetList();
                return;
            }
            else _lastProfile = profile;
        }
        else _lastProfile = profile;

        windowNameText.text = profile.windowName;
        actionButtonText.text = profile.actionButtonName;
        fileObjectPrefab = profile.fileObjectPrefab;
        directoryObjectPrefab = profile.directoryObjectPrefab;
        rootPath = profile.rootPath;
        showDirectories = profile.showDirectories;
        fileTypeTargets = profile.fileTypeTargets;
        
        _rootPath = PathTarget.GetPath((int)rootPath);
        _currentPath = _rootPath;
        _currentPathDisplay = GetComponentInChildren<TMP_InputField>();
        PathTarget.CheckPath(_rootPath);

        OpenWindow();
        ResetList();
    }
    
    public virtual void DeleteSelected()
    {
        if (FileListObject.SelectedFileListObject == null) return;
        WarningMessageBox.Instance.DisplayWarning("Are you sure you want to delete this?",ConfirmDeleteSelection);
    }

    public virtual void ConfirmDeleteSelection()
    {
        FileAttributes attr = File.GetAttributes(FileListObject.SelectedFileListObject.filePath);
        if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
            Directory.Delete(FileListObject.SelectedFileListObject.filePath,true);
        else
            File.Delete(FileListObject.SelectedFileListObject.filePath);
        FileListObject.SelectedFileListObject.Deselect();
        RefreshList();
    }
    
    public virtual void CreateFolder()
    {
        // Custom solution
        Action[] actions = new Action[1];
        actions[0] = delegate { ConfirmCreateFolder(InputPromptWindow.Instance.GetInputText()); };
        InputPromptWindow.Instance.OpenWindow();
        InputPromptWindow.Instance.SetupInputPromptWindow("New Folder",actions);
    }

    private void ConfirmCreateFolder(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            WarningMessageBox.Instance.DisplayWarning("Please enter name for new directory...");
            return;
        }
        Directory.CreateDirectory(_currentPath +"\\"+ fileName);
        RefreshList();
    }
    
    protected virtual void GotoFolder(string dir)
    {
        _currentPath = dir;
        RefreshList();
    }
    
    private void OnApplicationFocus(bool hasFocus)
    {
        if(hasFocus)
        {
            CloseWindow();
            OpenWindow();
        }
    }
}

[Serializable]
public class FileExplorerTarget
{
    public string fileType;
    public Sprite fileIcon;
}