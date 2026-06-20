using System;
using UnityEngine;
using UnityEngine.UI;

public sealed class ClayFormingRecipeSelectUi
{
    public event Action<string> RecipeSelected;
    public event Action Closed;

    public bool IsOpen { get; private set; }

    private RectTransform root;

    public void Build(Transform parent)
    {
        root = HudUiFactory.CreatePanel(
            parent,
            "ClayRecipeMenu",
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero);
        root.sizeDelta = new Vector2(260f, 180f);

        var background = root.gameObject.AddComponent<Image>();
        background.color = new Color(0f, 0f, 0f, 0.82f);

        HudUiFactory.CreateText(
            root,
            "Clay Forming",
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, -10f),
            18,
            TextAnchor.UpperCenter);

        HudUiFactory.CreateText(
            root,
            "Select item to craft",
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, -34f),
            13,
            TextAnchor.UpperCenter);

        var y = -64f;
        foreach (var recipe in ClayFormingRecipeLibrary.All)
        {
            CreateRecipeButton(root, recipe, y);
            y -= 44f;
        }

        CreateButton(root, "Cancel", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 12f), () =>
        {
            SetOpen(false);
            Closed?.Invoke();
        });

        SetOpen(false);
    }

    public void SetOpen(bool open)
    {
        IsOpen = open;
        if (root != null)
        {
            root.gameObject.SetActive(open);
        }
    }

    private void CreateRecipeButton(RectTransform parent, ClayFormingRecipe recipe, float y)
    {
        CreateButton(parent, recipe.DisplayName, new Vector2(0.1f, 1f), new Vector2(0.9f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, y), () =>
        {
            RecipeSelected?.Invoke(recipe.Id);
            SetOpen(false);
        });
    }

    private static void CreateButton(
        RectTransform parent,
        string label,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 anchoredPosition,
        Action onClick)
    {
        var buttonObject = new GameObject(label + "Button");
        buttonObject.transform.SetParent(parent, false);
        var rect = buttonObject.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(0f, 32f);

        var image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.22f, 0.22f, 0.22f, 0.95f);

        var button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(() => onClick());

        var textObject = new GameObject("Label");
        textObject.transform.SetParent(buttonObject.transform, false);
        var textRect = textObject.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        var text = textObject.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 14;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.text = label;
    }
}
