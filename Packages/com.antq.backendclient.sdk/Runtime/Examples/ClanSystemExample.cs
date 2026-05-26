using UnityEngine;
using Zenject;
using GameBackendModule.DI;
using GameBackendModule.Services;
using GameBackendModule.Models;

namespace GameBackendModule.Examples
{
    /// <summary>
    /// Ví dụ về cách sử dụng Clan System trong game
    /// </summary>
    public class ClanSystemExample : MonoBehaviour
    {
        [Inject] private IClanService clanService;
        [Inject] private IGameBackendManager backendManager;

        private void Start()
        {
            // Kiểm tra xem player đã đăng nhập chưa
            if (!backendManager.IsAuthenticated)
            {
                Debug.LogWarning("Cần đăng nhập để sử dụng clan system");
                return;
            }

            // Load clan data nếu player đã có clan
            LoadPlayerClanInfo();
        }

        #region Clan Management

        public void CreateNewClan(string clanName, string description, int maxMembers = 30, bool isPrivate = false)
        {
            var request = new CreateClanRequest
            {
                name = clanName,
                description = description,
                maxMembers = maxMembers,
                isPrivate = isPrivate
            };

            StartCoroutine(clanService.CreateClan(request,
                (response) => {
                    if (response.success)
                    {
                        Debug.Log($"Đã tạo clan thành công! ID: {response.data.id}");
                        Debug.Log($"Clan: {response.data.name} - {response.data.description}");
                        ShowClanCreatedUI(response.data);
                    }
                    else
                    {
                        Debug.LogError($"Không thể tạo clan: {response.message}");
                        ShowErrorUI($"Không thể tạo clan: {response.message}");
                    }
                },
                (error) => {
                    Debug.LogError($"Lỗi tạo clan: {error.message}");
                    ShowErrorUI($"Lỗi tạo clan: {error.message}");
                }));
        }

        public void SearchClans(string searchTerm)
        {
            var request = new SearchClanRequest
            {
                name = searchTerm,
                limit = 20,
                offset = 0
            };

            StartCoroutine(clanService.SearchClans(request,
                (response) => {
                    if (response.success)
                    {
                        Debug.Log($"Tìm thấy {response.data.Length} clan:");
                        foreach (var clan in response.data)
                        {
                            Debug.Log($"- {clan.name}: {clan.description} ({clan.memberCount}/{clan.maxMembers} thành viên)");
                        }
                        ShowClanSearchResults(response.data);
                    }
                    else
                    {
                        Debug.LogError($"Không thể tìm kiếm clan: {response.message}");
                        ShowErrorUI($"Không thể tìm kiếm clan: {response.message}");
                    }
                },
                (error) => {
                    Debug.LogError($"Lỗi tìm kiếm clan: {error.message}");
                    ShowErrorUI($"Lỗi tìm kiếm clan: {error.message}");
                }));
        }

        public void JoinClan(string clanId)
        {
            var request = new JoinClanRequest
            {
                clanId = clanId
            };

            StartCoroutine(clanService.JoinClan(request,
                (response) => {
                    if (response.success)
                    {
                        Debug.Log($"Đã tham gia clan thành công!");
                        LoadPlayerClanInfo();
                        ShowClanJoinedUI();
                    }
                    else
                    {
                        Debug.LogError($"Không thể tham gia clan: {response.message}");
                        ShowErrorUI($"Không thể tham gia clan: {response.message}");
                    }
                },
                (error) => {
                    Debug.LogError($"Lỗi tham gia clan: {error.message}");
                    ShowErrorUI($"Lỗi tham gia clan: {error.message}");
                }));
        }

        public void LeaveClan(string clanId)
        {
            var request = new LeaveClanRequest
            {
                clanId = clanId
            };

            StartCoroutine(clanService.LeaveClan(request,
                (response) => {
                    if (response.success)
                    {
                        Debug.Log($"Đã rời clan thành công!");
                        LoadPlayerClanInfo();
                        ShowClanLeftUI();
                    }
                    else
                    {
                        Debug.LogError($"Không thể rời clan: {response.message}");
                        ShowErrorUI($"Không thể rời clan: {response.message}");
                    }
                },
                (error) => {
                    Debug.LogError($"Lỗi rời clan: {error.message}");
                    ShowErrorUI($"Lỗi rời clan: {error.message}");
                }));
        }

        #endregion

        #region Clan Details

        public void LoadClanDetails(string clanId)
        {
            StartCoroutine(clanService.GetClanDetails(clanId,
                (response) => {
                    if (response.success)
                    {
                        var clan = response.data;
                        Debug.Log($"Clan Details:");
                        Debug.Log($"- Name: {clan.name}");
                        Debug.Log($"- Description: {clan.description}");
                        Debug.Log($"- Members: {clan.memberCount}/{clan.maxMembers}");
                        Debug.Log($"- Total Score: {clan.totalScore}");
                        Debug.Log($"- Private: {clan.isPrivate}");
                        
                        ShowClanDetailsUI(clan);
                        
                        // Load clan members
                        LoadClanMembers(clanId);
                    }
                    else
                    {
                        Debug.LogError($"Không thể lấy thông tin clan: {response.message}");
                        ShowErrorUI($"Không thể lấy thông tin clan: {response.message}");
                    }
                },
                (error) => {
                    Debug.LogError($"Lỗi lấy thông tin clan: {error.message}");
                    ShowErrorUI($"Lỗi lấy thông tin clan: {error.message}");
                }));
        }

        public void LoadClanMembers(string clanId)
        {
            StartCoroutine(clanService.GetClanMembers(clanId,
                (response) => {
                    if (response.success)
                    {
                        Debug.Log($"Clan Members ({response.data.Length}):");
                        foreach (var member in response.data)
                        {
                            Debug.Log($"- {member.userName} ({member.role}) - Contribution: {member.contribution}");
                        }
                        ShowClanMembersUI(response.data);
                    }
                    else
                    {
                        Debug.LogError($"Không thể lấy danh sách thành viên: {response.message}");
                        ShowErrorUI($"Không thể lấy danh sách thành viên: {response.message}");
                    }
                },
                (error) => {
                    Debug.LogError($"Lỗi lấy danh sách thành viên: {error.message}");
                    ShowErrorUI($"Lỗi lấy danh sách thành viên: {error.message}");
                }));
        }

        #endregion

        #region Clan Administration

        public void KickMember(string clanId, string userId)
        {
            var request = new KickMemberRequest
            {
                userId = userId
            };

            StartCoroutine(clanService.KickMember(clanId, request,
                (response) => {
                    if (response.success)
                    {
                        Debug.Log($"Đã kick thành viên thành công!");
                        LoadClanMembers(clanId); // Refresh member list
                        ShowMemberKickedUI();
                    }
                    else
                    {
                        Debug.LogError($"Không thể kick thành viên: {response.message}");
                        ShowErrorUI($"Không thể kick thành viên: {response.message}");
                    }
                },
                (error) => {
                    Debug.LogError($"Lỗi kick thành viên: {error.message}");
                    ShowErrorUI($"Lỗi kick thành viên: {error.message}");
                }));
        }

        public void PromoteMember(string clanId, string userId, string newRole)
        {
            var request = new PromoteMemberRequest
            {
                userId = userId,
                role = newRole
            };

            StartCoroutine(clanService.PromoteMember(clanId, request,
                (response) => {
                    if (response.success)
                    {
                        Debug.Log($"Đã thăng chức thành viên thành công!");
                        LoadClanMembers(clanId); // Refresh member list
                        ShowMemberPromotedUI();
                    }
                    else
                    {
                        Debug.LogError($"Không thể thăng chức thành viên: {response.message}");
                        ShowErrorUI($"Không thể thăng chức thành viên: {response.message}");
                    }
                },
                (error) => {
                    Debug.LogError($"Lỗi thăng chức thành viên: {error.message}");
                    ShowErrorUI($"Lỗi thăng chức thành viên: {error.message}");
                }));
        }

        public void UpdateClanInfo(string clanId, string newName, string newDescription, int newMaxMembers, bool newIsPrivate)
        {
            var request = new UpdateClanRequest
            {
                name = newName,
                description = newDescription,
                maxMembers = newMaxMembers,
                isPrivate = newIsPrivate
            };

            StartCoroutine(clanService.UpdateClan(clanId, request,
                (response) => {
                    if (response.success)
                    {
                        Debug.Log($"Đã cập nhật thông tin clan thành công!");
                        LoadClanDetails(clanId); // Refresh clan details
                        ShowClanUpdatedUI();
                    }
                    else
                    {
                        Debug.LogError($"Không thể cập nhật thông tin clan: {response.message}");
                        ShowErrorUI($"Không thể cập nhật thông tin clan: {response.message}");
                    }
                },
                (error) => {
                    Debug.LogError($"Lỗi cập nhật thông tin clan: {error.message}");
                    ShowErrorUI($"Lỗi cập nhật thông tin clan: {error.message}");
                }));
        }

        #endregion

        #region Player Clan Info

        private void LoadPlayerClanInfo()
        {
            // Trong thực tế, bạn sẽ cần một API endpoint để lấy clan của player hiện tại
            // Hoặc lưu thông tin clan trong player profile
            Debug.Log("Loading player clan info...");
            
            // Ví dụ: Giả sử player có clan ID được lưu trong profile
            string playerClanId = GetPlayerClanId();
            if (!string.IsNullOrEmpty(playerClanId))
            {
                LoadClanDetails(playerClanId);
            }
            else
            {
                Debug.Log("Player chưa có clan");
                ShowNoClanUI();
            }
        }

        private string GetPlayerClanId()
        {
            // Trong thực tế, bạn sẽ lấy thông tin này từ player profile hoặc API
            // Ở đây chỉ là ví dụ
            return PlayerPrefs.GetString("player_clan_id", "");
        }

        private void SetPlayerClanId(string clanId)
        {
            PlayerPrefs.SetString("player_clan_id", clanId);
            PlayerPrefs.Save();
        }

        #endregion

        #region UI Methods (Placeholder)

        private void ShowClanCreatedUI(ClanData clan)
        {
            Debug.Log($"UI: Clan '{clan.name}' đã được tạo thành công!");
            SetPlayerClanId(clan.id);
        }

        private void ShowClanSearchResults(ClanData[] clans)
        {
            Debug.Log($"UI: Hiển thị {clans.Length} clan tìm được");
        }

        private void ShowClanJoinedUI()
        {
            Debug.Log("UI: Đã tham gia clan thành công!");
        }

        private void ShowClanLeftUI()
        {
            Debug.Log("UI: Đã rời clan thành công!");
            SetPlayerClanId("");
        }

        private void ShowClanDetailsUI(ClanData clan)
        {
            Debug.Log($"UI: Hiển thị thông tin clan '{clan.name}'");
        }

        private void ShowClanMembersUI(ClanMember[] members)
        {
            Debug.Log($"UI: Hiển thị {members.Length} thành viên");
        }

        private void ShowMemberKickedUI()
        {
            Debug.Log("UI: Thành viên đã được kick");
        }

        private void ShowMemberPromotedUI()
        {
            Debug.Log("UI: Thành viên đã được thăng chức");
        }

        private void ShowClanUpdatedUI()
        {
            Debug.Log("UI: Thông tin clan đã được cập nhật");
        }

        private void ShowNoClanUI()
        {
            Debug.Log("UI: Player chưa có clan");
        }

        private void ShowErrorUI(string message)
        {
            Debug.LogError($"UI Error: {message}");
        }

        #endregion

        #region Public Methods for UI Buttons

        public void OnCreateClanButtonClicked()
        {
            // Ví dụ tạo clan với thông tin mặc định
            CreateNewClan("My New Clan", "A great clan for testing", 30, false);
        }

        public void OnSearchClansButtonClicked(string searchTerm)
        {
            SearchClans(searchTerm);
        }

        public void OnJoinClanButtonClicked(string clanId)
        {
            JoinClan(clanId);
        }

        public void OnLeaveClanButtonClicked(string clanId)
        {
            LeaveClan(clanId);
        }

        public void OnKickMemberButtonClicked(string clanId, string userId)
        {
            KickMember(clanId, userId);
        }

        public void OnPromoteMemberButtonClicked(string clanId, string userId, string newRole)
        {
            PromoteMember(clanId, userId, newRole);
        }

        public void OnUpdateClanButtonClicked(string clanId, string newName, string newDescription, int newMaxMembers, bool newIsPrivate)
        {
            UpdateClanInfo(clanId, newName, newDescription, newMaxMembers, newIsPrivate);
        }

        #endregion
    }
}
