using UnityEngine;
using Zenject;
using GameBackendModule.DI;
using GameBackendModule.Models;
using System.Collections.Generic;

namespace GameBackendModule.Examples
{
    /// <summary>
    /// MonoBehaviour để khởi tạo Game Backend Module với Zenject
    /// Đặt script này vào một GameObject trong scene để tự động khởi tạo module
    /// </summary>
    public class GameBackendInitializer : MonoBehaviour
    {
        [SerializeField] private string baseUrl = ApiConstants.BASE_URL;
        
        private void Awake()
        {
            // Tạo Context và Installer
            var context = new GameObject("GameBackendContext").AddComponent<SceneContext>();
            var installer = new GameObject("GameBackendInstaller").AddComponent<GameBackendInstaller>();
            
            // Cấu hình installer
            installer.baseUrl = baseUrl;
            
            // Gắn installer vào context
            var installers = new List<MonoInstaller>();
            installers.Add(installer);
            context.Installers = installers;
            
            // Khởi tạo context
            context.Run();
            
            Debug.Log("Game Backend Module đã được khởi tạo!");
        }
    }
}
