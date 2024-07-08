using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Thry.ThryEditor.ShaderTranslations
{
    internal class AvatarMaterialTranslatorMaterialListItem : VisualElement
    {
        static VisualTreeAsset VisualTreeAsset
        {
            get
            {
                if(_visualTreeAsset == null)
                    _visualTreeAsset = Resources.Load<VisualTreeAsset>("Shader Translator/AvatarMaterialTranslatorListItem");
                return _visualTreeAsset;
            }
        }
        static VisualTreeAsset _visualTreeAsset;

        ListView ownerList;
        AvatarMaterialTranslator ownerAvatarMaterialTranslator;

        ObjectField objField;
        Button button;

        int elementIndex;

        public AvatarMaterialTranslatorMaterialListItem(ListView ownerListView, AvatarMaterialTranslator ownerTranslator)
        {
            VisualTreeAsset.CloneTree(this);
            objField = this.Q<ObjectField>();
            objField.SetEnabled(false);

            button = this.Q<Button>();

            ownerList = ownerListView;
            ownerAvatarMaterialTranslator = ownerTranslator;
        }

        public void Bind(Material material, int index)
        {
            elementIndex = index;
            objField.SetValueWithoutNotify(material);

            UnregisterCallback<ChangeEvent<UnityEngine.Object>>(ObjFieldChanged);
            RegisterCallback<ChangeEvent<UnityEngine.Object>>(ObjFieldChanged);

            button.UnregisterCallback<MouseUpEvent>(ButtonClicked);
            button.Q<Button>().RegisterCallback<MouseUpEvent>(ButtonClicked);
        }

        void ButtonClicked(MouseUpEvent evt)
        {
            ownerList.viewController.RemoveItem(elementIndex);
            ownerList.Rebuild();
            ownerAvatarMaterialTranslator.UpdateTranslationsList();
        }

        void ObjFieldChanged(ChangeEvent<UnityEngine.Object> evt)
        {
            ownerAvatarMaterialTranslator.materials[elementIndex] = evt.newValue as Material;
            if(evt.newValue != null || evt.previousValue != null)
                ownerAvatarMaterialTranslator.UpdateTranslationsList();
        }
    }
}
