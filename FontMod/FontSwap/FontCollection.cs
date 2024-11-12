using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace FontMod.FontSwap;

public class FontCollection : ICollection<FontDataModel>
{
    private readonly HashSet<FontDataModel> _fontDataModels = [];

    public int Count => _fontDataModels.Count;

    public bool IsReadOnly => false;

    public void Add(FontDataModel item)
    {
        if (item == null)
            throw new ArgumentNullException("Cannot add null item to FontCollection");

        if (_fontDataModels.Contains(item))
            throw new ArgumentException($"FontCollection already contains {item.Font.name}");

        _fontDataModels.Add(item);
    }

    public void Add(Font item) => Add(FontDataModel.CreateFromFont(item));
    public void AddFromFilePath(string fontPath) => Add(FontDataModel.CreateFromFontPath(fontPath));
    public void AddFromFolderPath(string folderPath)
    {
        if (!Directory.Exists(folderPath))
            throw new DirectoryNotFoundException($"Folder path not found: {folderPath}");

        foreach (var font in Directory.GetFiles(folderPath))
            AddFromFilePath(font);
    }

    public FontDataModel GetFontByName(string name)
    {
        var model = _fontDataModels.Where(x => x.Name == name).First();

        if (model == null)
            throw new ArgumentException($"Font name {name} not found in collection");

        return model;
    }

    public void Clear() => _fontDataModels.Clear();

    public bool Contains(FontDataModel item) => _fontDataModels.Contains(item);

    public void CopyTo(FontDataModel[] array, int arrayIndex) => _fontDataModels.CopyTo(array, arrayIndex);

    public IEnumerator<FontDataModel> GetEnumerator() => _fontDataModels.GetEnumerator();

    public bool Remove(FontDataModel item) => _fontDataModels.Remove(item);

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
