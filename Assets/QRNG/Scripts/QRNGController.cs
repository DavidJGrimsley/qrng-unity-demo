using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using QuantumApi.Unity;
using TMPro;
using UnityEngine;

namespace QRNG
{
    public sealed class QRNGController : MonoBehaviour
    {
        const int MonsterCatchThreshold = 38;
        const float HardwarePollIntervalSeconds = 15f;
        const float HardwareTimeoutSeconds = 900f;

        [Header("Quantum API")]
        [SerializeField] bool backendProxyMode = false;
        [SerializeField] string apiKey = "";
        [SerializeField] int timeoutSeconds = 20;

        [Header("IBM Hardware Character Selection")]
        [SerializeField] string hardwareBackendName = "ibm_fez";
        [SerializeField] string hardwareProfileName = "Unreal Engine Demos";

        [Header("Cards")]
        [SerializeField] QrngArcadeCard coinCard = new QrngArcadeCard();
        [SerializeField] QrngArcadeCard monsterCard = new QrngArcadeCard();
        [SerializeField] QrngArcadeCard lootCard = new QrngArcadeCard();
        [SerializeField] QrngArcadeCard characterCard = new QrngArcadeCard();

        readonly string[] characterNames = { "Ryu", "Chun-Li", "Ken", "Akuma" };
        readonly string[] characterRoles = { "wandering warrior", "interpol investigator", "flaming shoto", "satsui master" };

        string activeHardwareJobId = "";
        bool applicationQuitting;
        CancellationTokenSource hardwareCancellation;

        public bool BackendProxyMode => backendProxyMode;

        void Awake()
        {
            WireCard(coinCard, RequestCoinFlip);
            WireCard(monsterCard, RequestMonsterCatch);
            WireCard(lootCard, RequestLootDrop);
            WireCard(characterCard, RequestCharacterSelection);
            ResetAllCards();
        }

        void OnDisable()
        {
            if (!string.IsNullOrWhiteSpace(activeHardwareJobId))
            {
                _ = CancelActiveHardwareJobAsync(activeHardwareJobId);
            }

            hardwareCancellation?.Cancel();
        }

        void OnApplicationQuit()
        {
            applicationQuitting = true;
            if (!string.IsNullOrWhiteSpace(activeHardwareJobId))
            {
                _ = CancelActiveHardwareJobAsync(activeHardwareJobId);
            }
        }

        public void Configure(
            QrngArcadeCard coin,
            QrngArcadeCard monster,
            QrngArcadeCard loot,
            QrngArcadeCard character)
        {
            coinCard = coin;
            monsterCard = monster;
            lootCard = loot;
            characterCard = character;
            ResetAllCards();
        }

        public void RequestCoinFlip()
        {
            if (coinCard.IsBusy || !ValidateClientConfiguration(coinCard))
            {
                return;
            }

            _ = RunLocalQrngCardAsync(
                coinCard,
                0,
                1,
                "Flipping...",
                "Measuring one Hadamard bit from /random.",
                response =>
                {
                    var heads = response.value == 0;
                    coinCard.SetIconVariant(heads ? 0 : 1);
                    return new DemoResult
                    {
                        Title = heads ? "HEADS" : "TAILS",
                        Message = heads
                            ? "QRNG measured 0, so the coin lands heads."
                            : "QRNG measured 1, so the coin lands tails.",
                    };
                });
        }

        public void RequestMonsterCatch()
        {
            if (monsterCard.IsBusy || !ValidateClientConfiguration(monsterCard))
            {
                return;
            }

            _ = RunLocalQrngCardAsync(
                monsterCard,
                1,
                100,
                "Throwing capsule...",
                "Rolling QRNG 1-100 against a fixed catch threshold.",
                response =>
                {
                    var caught = response.value <= MonsterCatchThreshold;
                    monsterCard.SetIconVariant(caught ? 1 : 0);
                    return new DemoResult
                    {
                        Title = caught ? "CAUGHT!" : "BROKE FREE",
                        Message = $"Roll {response.value}/100; catch succeeds on {MonsterCatchThreshold} or lower.",
                    };
                });
        }

        public void RequestLootDrop()
        {
            if (lootCard.IsBusy || !ValidateClientConfiguration(lootCard))
            {
                return;
            }

            _ = RunLocalQrngCardAsync(
                lootCard,
                1,
                1000,
                "Opening chest...",
                "Rolling QRNG 1-1000 through weighted rarity bands.",
                response =>
                {
                    var rarity = LootRarityFromRoll(response.value);
                    lootCard.SetIconVariant(rarity.IconVariant);
                    return new DemoResult
                    {
                        Title = rarity.Name,
                        Message = $"Roll {response.value}/1000 landed in the {rarity.RangeLabel} band.",
                    };
                });
        }

        public void RequestCharacterSelection()
        {
            if (characterCard.IsBusy || !ValidateClientConfiguration(characterCard))
            {
                return;
            }

            _ = RunHardwareCharacterAsync();
        }

        QuantumApiClient CreateClient()
        {
            return new QuantumApiClient(new QuantumApiClientOptions
            {
                BackendProxyMode = backendProxyMode,
                ApiKey = backendProxyMode ? "" : apiKey?.Trim(),
                DefaultAuthMode = QuantumApiAuthMode.Auto,
                TimeoutSeconds = timeoutSeconds > 0 ? timeoutSeconds : 20,
            });
        }

        bool ValidateClientConfiguration(QrngArcadeCard card)
        {
            if (!backendProxyMode && string.IsNullOrWhiteSpace(apiKey))
            {
                card.ShowError("API key is missing on the scene object. Select QRNGController and paste it into the Inspector API Key field.");
                return false;
            }

            return true;
        }

        async Task RunLocalQrngCardAsync(
            QrngArcadeCard card,
            int min,
            int max,
            string busyTitle,
            string busyMessage,
            Func<RandomIntResponse, DemoResult> mapResult)
        {
            card.SetBusy(busyTitle, busyMessage);

            try
            {
                var response = await CreateClient().RandomIntAsync(min, max).ConfigureAwait(true);
                if (response.value < min || response.value > max)
                {
                    throw new InvalidOperationException($"The QRNG returned {response.value}, outside the expected {min}-{max} range.");
                }

                var result = mapResult(response);
                card.ShowSuccess(result.Title, result.Message, $"Source: {CleanSource(response.source)}");
            }
            catch (QuantumApiError error)
            {
                card.ShowError(FormatQuantumError(error));
            }
            catch (Exception error)
            {
                card.ShowError(FriendlyException(error));
            }
            finally
            {
                card.SetIdle();
            }
        }

        async Task RunHardwareCharacterAsync()
        {
            hardwareCancellation?.Cancel();
            hardwareCancellation = new CancellationTokenSource();
            var token = hardwareCancellation.Token;

            characterCard.SetBusy(
                "Submitting IBM job...",
                $"Sending a 2-bit random selection job to {hardwareBackendName}. This may take several minutes.");

            try
            {
                var client = CreateClient();
                var submit = await client.SubmitRandomJobAsync(
                    new RandomJobSubmitRequest
                    {
                        min = 0,
                        max = 3,
                        provider = "ibm",
                        backend_name = hardwareBackendName,
                        ibm_profile = hardwareProfileName,
                    },
                    new QuantumApiRequestOptions { TimeoutSeconds = 45 }).ConfigureAwait(true);

                activeHardwareJobId = submit.job_id;
                characterCard.ShowProgress(
                    "IBM job queued",
                    $"Job {ShortJobId(activeHardwareJobId)} is {ReadableStatus(submit.status)} on {submit.backend_name}.",
                    $"Profile: {submit.ibm_profile}");

                var startedAt = Time.realtimeSinceStartup;
                while (!token.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromSeconds(HardwarePollIntervalSeconds), token).ConfigureAwait(true);

                    RandomJobStatusResponse status;
                    try
                    {
                        status = await client.GetJobAsync(
                            activeHardwareJobId,
                            new QuantumApiRequestOptions { TimeoutSeconds = 60 }).ConfigureAwait(true);
                    }
                    catch (QuantumApiError error) when (IsTransientHardwarePollError(error))
                    {
                        characterCard.ShowProgress(
                            "Still checking IBM job",
                            $"The API status request timed out, but job {ShortJobId(activeHardwareJobId)} may still be running. Polling again in {HardwarePollIntervalSeconds:0} seconds.",
                            "Remote: pending");
                        continue;
                    }

                    characterCard.ShowProgress(
                        HardwareStatusTitle(status.status),
                        $"Job {ShortJobId(activeHardwareJobId)} is {ReadableStatus(status.status)} on {status.backend_name}.",
                        $"Remote: {ShortJobId(status.remote_job_id)}");

                    if (IsTerminalStatus(status.status))
                    {
                        if (string.Equals(status.status, "succeeded", StringComparison.OrdinalIgnoreCase))
                        {
                            try
                            {
                                var result = await client.GetJobResultAsync(
                                    activeHardwareJobId,
                                    new QuantumApiRequestOptions { TimeoutSeconds = 60 }).ConfigureAwait(true);
                                ShowCharacterResult(result);
                            }
                            catch (QuantumApiError error) when (IsTransientHardwarePollError(error))
                            {
                                characterCard.ShowProgress(
                                    "IBM result pending",
                                    $"IBM says job {ShortJobId(activeHardwareJobId)} succeeded, but the result endpoint timed out. Polling again in {HardwarePollIntervalSeconds:0} seconds.",
                                    $"Remote: {ShortJobId(status.remote_job_id)}");
                                continue;
                            }
                        }
                        else if (string.Equals(status.status, "cancelled", StringComparison.OrdinalIgnoreCase))
                        {
                            characterCard.ShowError("The IBM hardware job was cancelled before a result was returned.");
                        }
                        else
                        {
                            var reason = status.error != null && !string.IsNullOrWhiteSpace(status.error.message)
                                ? status.error.message
                                : "The IBM hardware job failed before returning a result.";
                            characterCard.ShowError(reason);
                        }

                        activeHardwareJobId = "";
                        return;
                    }

                    if (Time.realtimeSinceStartup - startedAt > HardwareTimeoutSeconds)
                    {
                        await CancelActiveHardwareJobAsync(activeHardwareJobId).ConfigureAwait(true);
                        characterCard.ShowError("IBM hardware job timed out locally after 15 minutes and a cancellation was requested.");
                        activeHardwareJobId = "";
                        return;
                    }
                }
            }
            catch (TaskCanceledException)
            {
                if (!applicationQuitting)
                {
                    characterCard.ShowError("The IBM hardware selection was cancelled.");
                }
            }
            catch (QuantumApiError error)
            {
                characterCard.ShowError(FormatQuantumError(error));
            }
            catch (Exception error)
            {
                characterCard.ShowError(FriendlyException(error));
            }
            finally
            {
                characterCard.SetIdle();
            }
        }

        void ShowCharacterResult(RandomJobResultResponse response)
        {
            var value = Mathf.Clamp(response.result.value, 0, characterNames.Length - 1);
            characterCard.SetIconVariant(value);
            characterCard.ShowSuccess(
                characterNames[value].ToUpperInvariant(),
                $"IBM hardware measured value {value}, selecting {characterNames[value]} the {characterRoles[value]}.",
                $"Source: {CleanSource(response.result.source)}");
        }

        async Task CancelActiveHardwareJobAsync(string jobId)
        {
            if (string.IsNullOrWhiteSpace(jobId))
            {
                return;
            }

            try
            {
                await CreateClient().CancelJobAsync(
                    jobId,
                    new QuantumApiRequestOptions { TimeoutSeconds = 15 }).ConfigureAwait(false);
            }
            catch
            {
                // Best-effort cancellation only; quitting the game should not surface a late error.
            }
        }

        void ResetAllCards()
        {
            coinCard.Reset(
                "Coin Flip",
                "One QRNG bit from /random maps 0 → heads and 1 → tails.",
                "Flip",
                ArcadeIconKind.Coin);
            monsterCard.Reset(
                "Catch Monster",
                $"A QRNG roll from 1-100 succeeds on {MonsterCatchThreshold} or lower.",
                "Throw",
                ArcadeIconKind.Monster);
            lootCard.Reset(
                "Loot Drop",
                "A QRNG roll from 1-1000 maps into weighted rarity bands.",
                "Open",
                ArcadeIconKind.Chest);
            characterCard.Reset(
                "Random Character",
                $"IBM hardware job on {hardwareBackendName}. Two Hadamard-measured bits choose one of four Street Fighter characters and may take several minutes.",
                "Select",
                ArcadeIconKind.Character);
        }

        static void WireCard(QrngArcadeCard card, UnityEngine.Events.UnityAction action)
        {
            if (card?.actionButton == null)
            {
                return;
            }

            card.actionButton.onClick.RemoveAllListeners();
            card.actionButton.onClick.AddListener(action);
        }

        static LootRarity LootRarityFromRoll(int value)
        {
            if (value >= 996)
            {
                return new LootRarity("MYTHIC", "996-1000", 4);
            }

            if (value >= 951)
            {
                return new LootRarity("LEGENDARY", "951-995", 3);
            }

            if (value >= 801)
            {
                return new LootRarity("RARE", "801-950", 2);
            }

            if (value >= 451)
            {
                return new LootRarity("UNCOMMON", "451-800", 1);
            }

            return new LootRarity("COMMON", "1-450", 0);
        }

        static bool IsTerminalStatus(string status)
        {
            return string.Equals(status, "succeeded", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(status, "failed", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(status, "cancelled", StringComparison.OrdinalIgnoreCase);
        }

        static string HardwareStatusTitle(string status)
        {
            if (string.Equals(status, "running", StringComparison.OrdinalIgnoreCase))
            {
                return "IBM job running";
            }

            if (string.Equals(status, "succeeded", StringComparison.OrdinalIgnoreCase))
            {
                return "IBM result ready";
            }

            return "IBM job queued";
        }

        static string ReadableStatus(string status)
        {
            return string.IsNullOrWhiteSpace(status) ? "pending" : status.Replace("_", " ").ToLowerInvariant();
        }

        static string ShortJobId(string jobId)
        {
            if (string.IsNullOrWhiteSpace(jobId))
            {
                return "pending";
            }

            return jobId.Length <= 8 ? jobId : jobId.Substring(0, 8) + "...";
        }

        static bool IsTransientHardwarePollError(QuantumApiError error)
        {
            if (error == null)
            {
                return false;
            }

            if (error.StatusCode == 408 || error.StatusCode == 504)
            {
                return true;
            }

            return !string.IsNullOrWhiteSpace(error.Message) &&
                   (error.Message.IndexOf("maximum execution time", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    error.Message.IndexOf("request exceeded", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    error.Message.IndexOf("timed out", StringComparison.OrdinalIgnoreCase) >= 0);
        }

        static string CleanSource(string source)
        {
            return string.IsNullOrWhiteSpace(source) ? "unknown" : source.Trim();
        }

        static string FriendlyException(Exception error)
        {
            return string.IsNullOrWhiteSpace(error?.Message)
                ? "QRNG request failed. Check the API connection and try again."
                : error.Message;
        }

        static string FormatQuantumError(QuantumApiError error)
        {
            if (error == null)
            {
                return "QRNG request failed. Check the API connection and try again.";
            }

            if (error.ErrorCode == "missing_api_key" ||
                (!string.IsNullOrWhiteSpace(error.Message) &&
                 error.Message.IndexOf("requires an API key", StringComparison.OrdinalIgnoreCase) >= 0))
            {
                return "API key is not available to the scene object. Select QRNGController and set API Key in the Inspector.";
            }

            if (error.RetryAfter.HasValue)
            {
                return $"Quantum API is rate-limiting this key. Try again in about {error.RetryAfter.Value} seconds.";
            }

            if (error.ErrorCode == "profile_not_found")
            {
                return "IBM profile \"Unreal Engine Demos\" was not found for this API key owner.";
            }

            if (error.ErrorCode == "hardware_backend_required")
            {
                return "The selected IBM backend is not reporting as hardware. Check ibm_fez access/availability.";
            }

            if (error.StatusCode == 401 || error.StatusCode == 403)
            {
                return "The API rejected this request. Check the API key and IBM profile access.";
            }

            if (error.StatusCode == 503)
            {
                return "The hardware provider is unavailable right now. Try again later.";
            }

            return string.IsNullOrWhiteSpace(error.Message)
                ? "Quantum API request failed. Try again."
                : error.Message;
        }

        struct DemoResult
        {
            public string Title;
            public string Message;
        }

        readonly struct LootRarity
        {
            public readonly string Name;
            public readonly string RangeLabel;
            public readonly int IconVariant;

            public LootRarity(string name, string rangeLabel, int iconVariant)
            {
                Name = name;
                RangeLabel = rangeLabel;
                IconVariant = iconVariant;
            }
        }
    }

    [Serializable]
    public sealed class QrngArcadeCard
    {
        public TextMeshProUGUI titleLabel;
        public TextMeshProUGUI captionLabel;
        public TextMeshProUGUI resultLabel;
        public TextMeshProUGUI statusLabel;
        public TextMeshProUGUI sourceLabel;
        public TextMeshProUGUI buttonLabel;
        public UnityEngine.UI.Button actionButton;
        public RectTransform visualRoot;
        public ArcadeIconGraphic iconGraphic;

        Coroutine animation;
        MonoBehaviour animationHost;

        public bool IsBusy { get; private set; }

        public void Reset(string title, string caption, string buttonText, ArcadeIconKind iconKind)
        {
            SetText(titleLabel, title);
            SetText(captionLabel, caption);
            SetText(buttonLabel, buttonText);
            SetText(resultLabel, "READY");
            SetText(statusLabel, "Press the button to request a QRNG result.");
            SetText(sourceLabel, "Source: waiting");
            if (iconGraphic != null)
            {
                iconGraphic.Kind = iconKind;
                iconGraphic.Variant = 0;
            }

            SetIdle();
        }

        public void SetBusy(string title, string message)
        {
            IsBusy = true;
            SetText(resultLabel, title);
            SetText(statusLabel, message);
            SetText(sourceLabel, "Source: pending");
            if (actionButton != null)
            {
                actionButton.interactable = false;
            }

            if (visualRoot != null)
            {
                animationHost = visualRoot.GetComponentInParent<MonoBehaviour>();
                if (animationHost != null)
                {
                    animation = animationHost.StartCoroutine(Animate());
                }
            }
        }

        public void SetIdle()
        {
            IsBusy = false;
            if (actionButton != null)
            {
                actionButton.interactable = true;
            }

            if (animationHost != null && animation != null)
            {
                animationHost.StopCoroutine(animation);
            }

            animation = null;
            animationHost = null;
            if (visualRoot != null)
            {
                visualRoot.localScale = Vector3.one;
                visualRoot.localEulerAngles = Vector3.zero;
            }
        }

        public void ShowProgress(string title, string message, string source)
        {
            SetText(resultLabel, title);
            SetText(statusLabel, message);
            SetText(sourceLabel, source);
        }

        public void ShowSuccess(string title, string message, string source)
        {
            SetText(resultLabel, title);
            SetText(statusLabel, message);
            SetText(sourceLabel, source);
        }

        public void ShowError(string message)
        {
            SetText(resultLabel, "TRY AGAIN");
            SetText(statusLabel, string.IsNullOrWhiteSpace(message) ? "QRNG request failed. Try again." : message);
            SetText(sourceLabel, "Source: unavailable");
            SetIdle();
        }

        public void SetIconVariant(int variant)
        {
            if (iconGraphic == null)
            {
                return;
            }

            iconGraphic.Variant = variant;
        }

        IEnumerator Animate()
        {
            var elapsed = 0f;
            while (IsBusy)
            {
                elapsed += Time.unscaledDeltaTime;
                if (visualRoot != null)
                {
                    visualRoot.localScale = Vector3.one * (1f + Mathf.Sin(elapsed * 7f) * 0.035f);
                    visualRoot.localEulerAngles = new Vector3(0f, Mathf.Sin(elapsed * 8f) * 5f, Mathf.Sin(elapsed * 5f) * 3f);
                }

                if (iconGraphic != null && iconGraphic.Kind == ArcadeIconKind.Character)
                {
                    iconGraphic.Variant = Mathf.FloorToInt(elapsed * 4f) % 4;
                }

                yield return null;
            }
        }

        static void SetText(TextMeshProUGUI label, string value)
        {
            if (label != null)
            {
                label.text = value ?? "";
            }
        }
    }
}
