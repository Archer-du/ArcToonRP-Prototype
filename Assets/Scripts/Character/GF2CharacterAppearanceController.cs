using System;
using System.Collections.Generic;
using UnityEngine;

namespace ArcToon.Scripts.Character
{
    public enum GF2ClothType
    {
        Fight,
        Dorm,
    }
    
    [ExecuteInEditMode]
    public class GF2CharacterAppearanceController : MonoBehaviour
    {
        public GF2ClothType clothType;
        public GF2ClothType shoesType;

        public bool showWeapon = false;
        
        public List<SkinnedMeshRenderer> clothFightMeshRenderers;
        public List<SkinnedMeshRenderer> clothDormMeshRenderers;
        public List<SkinnedMeshRenderer> shoesFightMeshRenderers;
        public List<SkinnedMeshRenderer> shoesDormMeshRenderers;

        public GameObject weaponObject;

        private void Update()
        {
            UpdateCharacterAppearance();
        }

        private void UpdateCharacterAppearance()
        {
            UpdateClothRenderers();
            UpdateShoesRenderers();
            UpdateWeaponVisibility();
        }

        private void UpdateClothRenderers()
        {
            if (clothFightMeshRenderers != null)
            {
                foreach (SkinnedMeshRenderer meshRenderer in clothFightMeshRenderers)
                {
                    if(meshRenderer != null)
                        meshRenderer.enabled = clothType == GF2ClothType.Fight;
                }
            }

            if (clothDormMeshRenderers != null)
            {
                foreach (SkinnedMeshRenderer meshRenderer in clothDormMeshRenderers)
                {
                    if(meshRenderer != null)
                        meshRenderer.enabled = clothType == GF2ClothType.Dorm;
                }
            }
        }

        private void UpdateShoesRenderers()
        {
            if (shoesFightMeshRenderers != null)
            {
                foreach (SkinnedMeshRenderer meshRenderer in shoesFightMeshRenderers)
                {
                    if(meshRenderer != null)
                        meshRenderer.enabled = shoesType == GF2ClothType.Fight;
                }
            }

            if (shoesDormMeshRenderers != null)
            {
                foreach (SkinnedMeshRenderer meshRenderer in shoesDormMeshRenderers)
                {
                    if(meshRenderer != null)
                        meshRenderer.enabled = shoesType == GF2ClothType.Dorm;
                }
            }
        }

        private void UpdateWeaponVisibility()
        {
            if(weaponObject != null)
                weaponObject.SetActive(showWeapon);
        }
    }
}