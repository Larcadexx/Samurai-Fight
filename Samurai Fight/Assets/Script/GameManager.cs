// Salin semua kode dari sini
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement; // Untuk me-restart game atau kembali ke Main Menu
using System.Linq; // Untuk menggunakan fungsi .Any(), .First(), .Sum()

// Ini adalah OTAK dari game
public class GameManager : MonoBehaviour
{
    public static GameManager instance; // Singleton
    public UIManager uiManager; // Referensi ke UIManager

    [Header("Referensi Objek di Scene")]
    public GameObject pionManusia; // GameObject pion pemain
    public GameObject pionAI;      // GameObject pion AI
    public Transform[] petakPapan; // Array dari Transform petak (untuk menentukan posisi)

    // Variabel data game
    private Player manusia;
    private Player ai;
    private List<Card> deck = new List<Card>(); // Deck utama

    private Player activePlayer; // Pemain yang sedang giliran
    private bool isGameOver = false;

    // Variabel "State" (Status)
    // Ini melacak apa yang sedang pemain coba lakukan
    private bool isPlayerMelangkah, isPlayerMenyerang, isPlayerInSergapMode, isPlayerStrengtheningAttack, isPlayerInSerangBalikMode;
    private int arahLangkah = 0; // 1 untuk maju, -1 untuk mundur

    // State machine untuk fase serangan
    private enum AttackPhase { None, AwaitingTangkis, AwaitingSerangBalik, AwaitingTangkisPlayer, PlayerSelectingParryCards }
    private AttackPhase currentAttackPhase = AttackPhase.None;
    private bool isCurrentAttackACounter = false; // True jika serangan ini adalah serang balik

    // Variabel sementara untuk proses serangan
    private Player attacker;
    private Player defender;
    private int attackValue;
    private Card initialAttackCard; // Kartu pertama yang digunakan untuk menyerang
    // Daftar kartu yang dipilih pemain untuk menangkis
    private List<KeyValuePair<Card, SistemKartu>> selectedParryCards = new List<KeyValuePair<Card, SistemKartu>>();

    void Awake()
    {
        // Setup Singleton
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        SetupGame(); // Siapkan data pemain
        StartRound();  // Mulai ronde pertama
    }

    void SetupGame()
    {
        // Buat objek data pemain baru
        manusia = new Player("Manusia", 1); // Mulai di petak 1
        ai = new Player("AI", 23, true); // Mulai di petak 23
        uiManager.ResetScoreUI(); // Kosongkan UI skor
    }

    // Fungsi untuk memulai (atau me-restart) satu ronde
    public void StartRound()
    {
        // Reset semua state ke nilai default
        currentAttackPhase = AttackPhase.None;
        isPlayerMelangkah = false;
        isPlayerMenyerang = false;
        isPlayerInSergapMode = false;
        isPlayerStrengtheningAttack = false;
        isPlayerInSerangBalikMode = false;
        isCurrentAttackACounter = false;
        isGameOver = false;

        // Kembalikan pion ke posisi awal
        manusia.position = 1;
        ai.position = 23;
        MovePawnVisual(manusia, manusia.position); // Pindahkan visual pion
        MovePawnVisual(ai, ai.position);

        // Siapkan deck
        CreateDeck();
        ShuffleDeck();
        
        // Kosongkan tangan pemain
        manusia.hand.Clear();
        ai.hand.Clear();

        // Bagi 5 kartu ke masing-masing pemain
        for (int i = 0; i < 5; i++)
        {
            manusia.hand.Add(DrawCardFromDeck());
            ai.hand.Add(DrawCardFromDeck());
        }

        // Update UI
        uiManager.UpdatePlayerHandUI(manusia);
        uiManager.UpdateMainDeckUI(deck.Count);
        
        // Giliran pertama dimulai oleh AI
        activePlayer = ai;
        StartTurn(); // Mulai giliran
    }

    // Memulai giliran untuk 'activePlayer'
    private void StartTurn()
    {
        if (isGameOver) return; // Jika game sudah selesai, jangan lakukan apa-apa

        uiManager.RestoreDefaultLayout(); // Kembalikan UI ke tampilan default
        Debug.Log($"Sekarang giliran: {activePlayer.playerName}");

        if (activePlayer.isAI)
        {
            // Jika AI, jalankan logika AI
            StartCoroutine(ExecuteAITurnCoroutine());
        }
        else
        {
            // Jika Manusia
            uiManager.ShowMessage("Giliran Anda! Pilih aksi: Melangkah atau Serang.", 0f); // Tampilkan pesan
            CheckAvailablePlayerActions(); // Cek apakah pemain bisa melangkah/serang
            uiManager.OpsiAwalPanel.SetActive(true); // Tampilkan panel opsi
            
            // Tampilkan panel kartu pemain
            if (uiManager.KartuManusiaPanel != null) uiManager.KartuManusiaPanel.SetActive(true);

            // Nonaktifkan interaksi kartu (pemain harus pilih aksi dulu)
            uiManager.SetPlayerHandInteractable(false); 
            CameraManager.Instance.SwitchToPlayerTurn(); // Ganti kamera
        }
    }
    
    // Mengakhiri giliran
    private void EndTurn()
    {
        if (isGameOver) return;
        
        Debug.Log($"Giliran {activePlayer.playerName} berakhir.");
        RefillHand(manusia); // Isi ulang tangan manusia
        RefillHand(ai);      // Isi ulang tangan AI
        
        // Cek jika deck habis dan salah satu pemain tidak bisa isi ulang
        if (deck.Count == 0 && (manusia.hand.Count < 5 || ai.hand.Count < 5))
        {
            // Jika ya, mulai Tie Breaker
            StartCoroutine(ExecuteTieBreakerCoroutine());
            return;
        }
        
        // Ganti pemain aktif
        activePlayer = (activePlayer == manusia) ? ai : manusia;
        StartTurn(); // Mulai giliran baru
    }

    // --- FUNGSI INPUT PEMAIN (Dipanggil dari Tombol UI) ---

    // Dipanggil saat tombol "Melangkah" ditekan
    public void OnMelangkahButtonPressed()
    {
        if (activePlayer != manusia) return;
        isPlayerMelangkah = true; // Set state
        uiManager.ShowMessage("Pilih arah tujuan Anda.", 0f);
        uiManager.OpsiAwalPanel.SetActive(false); // Sembunyikan panel opsi
        
        // Sembunyikan panel kartu (agar tidak bingung)
        if (uiManager.KartuManusiaPanel != null) uiManager.KartuManusiaPanel.SetActive(false);
        
        uiManager.AksiMelangkahPanel.SetActive(true); // Tampilkan panel arah
        CameraManager.Instance.SwitchToPlayerTurn();
        CheckAvailableMoveDirections(); // Cek tombol maju/mundur
    }

    // Dipanggil saat tombol "Maju" (isMaju=true) atau "Mundur" (isMaju=false) ditekan
    public void OnArahLangkahPressed(bool isMaju)
    {
        if (activePlayer != manusia) return;
        uiManager.AksiMelangkahPanel.SetActive(false); // Sembunyikan panel arah
        arahLangkah = isMaju ? 1 : -1; // Set state arah
        CameraManager.Instance.SwitchToFollowMove();
        string arah = isMaju ? "maju" : "mundur";
        uiManager.ShowMessage($"Anda melangkah {arah}. Pilih satu kartu untuk menentukan jarak.", 0f);
        if (uiManager.KartuManusiaPanel != null) uiManager.KartuManusiaPanel.SetActive(true); // Tampilkan kartu
        uiManager.SetPlayerHandInteractable(true); // Kartu sekarang bisa diklik
    }

    // Dipanggil saat tombol "Serang" ditekan
    public void OnSerangButtonPressed()
    {
        if (activePlayer != manusia) return;
        uiManager.OpsiAwalPanel.SetActive(false);
        isPlayerMenyerang = true; // Set state
        uiManager.ShowMessage("Pilih kartu yang nilainya sama dengan jarak Anda ke lawan.", 0f);
        CameraManager.Instance.SwitchToAttack();
        if (uiManager.KartuManusiaPanel != null) uiManager.KartuManusiaPanel.SetActive(true);
        uiManager.SetPlayerHandInteractable(true); // Kartu bisa diklik
    }

    // --- LOGIKA UTAMA ---

    // Dipanggil oleh SistemKartu.cs saat kartu APAPUN diklik
    public void OnCardSlotClicked(Card clickedCard, SistemKartu slotController)
    {
        // BLOK 1: LOGIKA REAKTIF (Terjadi di luar giliran pemain)
        // Cek apakah pemain sedang dalam mode memilih kartu TANGKISAN?
        if (currentAttackPhase == AttackPhase.PlayerSelectingParryCards)
        {
            HandleParrySelection(clickedCard, slotController); // Proses pemilihan kartu tangkis
            return; // Selesai
        }

        // BLOK 2: LOGIKA AKSI (Saat giliran pemain ATAU Serang Balik)
        // Abaikan klik jika bukan giliran pemain DAN pemain tidak sedang serang balik
        if (activePlayer != manusia && !isPlayerInSerangBalikMode)
        {
            return;
        }

        // Pastikan ada state aksi yang aktif (melangkah, menyerang, dll.)
        if (!isPlayerMenyerang && !isPlayerMelangkah && !isPlayerInSergapMode && !isPlayerStrengtheningAttack && !isPlayerInSerangBalikMode)
            return;

        // Jika lolos semua cek, proses kartu
        uiManager.SetPlayerHandInteractable(false); // Matikan interaksi kartu
        uiManager.HideMessage(); // Sembunyikan pesan
        HandleCardAction(clickedCard); // Proses aksi berdasarkan state
    }

    // Memproses kartu yang diklik berdasarkan state saat ini
    private void HandleCardAction(Card clickedCard)
    {
        if (isPlayerMenyerang) // Jika state-nya "Menyerang"
        {
            int distance = ai.position - manusia.position; // Hitung jarak
            if (clickedCard.value == distance) // Cek apakah nilai kartu = jarak
            {
                // Berhasil
                isPlayerMenyerang = false; // Reset state
                initialAttackCard = clickedCard; // Simpan kartu serangan
                manusia.hand.Remove(clickedCard); // Buang kartu dari tangan
                uiManager.UpdatePlayerHandUI(manusia); // Update UI tangan
                
                if (manusia.hand.Count > 0) // Cek jika masih punya kartu
                {
                    // Tawarkan untuk perkuat serangan
                    if (uiManager.KartuManusiaPanel != null) uiManager.KartuManusiaPanel.SetActive(false);
                    uiManager.PerkuatSeranganPanel.SetActive(true);
                    uiManager.ShowMessage("Serangan awal siap. Ingin perkuat dengan kartu tambahan?", 0f);
                }
                else
                {
                    // Jika tidak punya kartu lagi, langsung serang
                    InitiateAttack(manusia, ai, initialAttackCard.value, false);
                }
            } else { 
                // Jika nilai kartu salah, batalkan giliran (atau bisa diubah)
                StartTurn(); 
            }
        }
        else if (isPlayerStrengtheningAttack) // Jika state-nya "Perkuat Serangan"
        {
            isPlayerStrengtheningAttack = false; // Reset state
            int totalAttackValue = initialAttackCard.value + clickedCard.value; // Hitung total serangan
            manusia.hand.Remove(clickedCard); // Buang kartu
            uiManager.UpdatePlayerHandUI(manusia);
            InitiateAttack(manusia, ai, totalAttackValue, false); // Mulai serangan
        }
        else if (isPlayerMelangkah) // Jika state-nya "Melangkah"
        {
            HandleMove(clickedCard); // Proses logika pindah
        }
        else if (isPlayerInSergapMode) // Jika state-nya "Sergap"
        {
            HandleSergap(clickedCard); // Proses logika sergap
        }
        else if (isPlayerInSerangBalikMode) // Jika state-nya "Serang Balik"
        {
            isPlayerInSerangBalikMode = false; // Reset state
            manusia.hand.Remove(clickedCard); // Buang kartu
            uiManager.UpdatePlayerHandUI(manusia);
            InitiateAttack(manusia, ai, clickedCard.value, true); // Mulai serangan balik
        }
    }

    // Logika khusus untuk aksi Melangkah
    private void HandleMove(Card clickedCard)
    {
        // Hitung posisi baru
        int newPosition = manusia.position + (clickedCard.value * arahLangkah);
        // Cek validitas: Maju (< AI pos) atau Mundur (>= 1)
        bool isValidMove = (arahLangkah == 1 && newPosition < ai.position) || (arahLangkah == -1 && newPosition >= 1);

        if (isValidMove)
        {
            manusia.position = newPosition; // Update data posisi
            MovePawnVisual(manusia, manusia.position); // Pindahkan visual pion
            manusia.hand.Remove(clickedCard); // Buang kartu
            uiManager.UpdatePlayerHandUI(manusia);
            isPlayerMelangkah = false; // Reset state

            // Cek "Sergap"
            int newDistance = ai.position - newPosition;
            // Cek apakah pemain punya kartu yang nilainya = jarak baru
            bool canSergap = manusia.hand.Any(card => card.value == newDistance); 
            if (canSergap)
            {
                // Tawarkan Sergap
                uiManager.ShowMessage("Langkah berhasil! Anda kini dalam jangkauan. Lakukan Sergap?", 0f);
                uiManager.AksiSergapPanel.SetActive(true);
            }
            else
            {
                EndTurn(); // Jika tidak bisa sergap, akhiri giliran
            }
        }
        else
        {
            // Jika langkah tidak valid, batalkan (kembali ke awal giliran)
            isPlayerMelangkah = false;
            StartTurn();
        }
    }

    // Logika khusus untuk aksi Sergap
    private void HandleSergap(Card clickedCard)
    {
        isPlayerInSergapMode = false; // Reset state
        int distance = ai.position - manusia.position;
        if (clickedCard.value == distance) // Cek kartu valid
        {
            manusia.hand.Remove(clickedCard);
            uiManager.UpdatePlayerHandUI(manusia);
            InitiateAttack(manusia, ai, clickedCard.value, false); // Mulai serangan
        }
        else
        {
            EndTurn(); // Jika kartu salah (seharusnya tidak terjadi), akhiri giliran
        }
    }

    // --- LOGIKA SERANG / TANGKIS ---

    // Fungsi utama untuk memulai fase serangan
    private void InitiateAttack(Player currentAttacker, Player currentDefender, int value, bool isCounter)
    {
        // Set variabel global untuk serangan ini
        attacker = currentAttacker;
        defender = currentDefender;
        attackValue = value;
        isCurrentAttackACounter = isCounter; // Tandai jika ini serang balik

        uiManager.ShowAttackStrength(attackValue); // Tampilkan UI kekuatan serangan

        if (defender.isAI) // Jika yang diserang adalah AI
        {
            currentAttackPhase = AttackPhase.AwaitingTangkis;
            StartCoroutine(DecideAITangkisCoroutine()); // AI akan memutuskan
        }
        else // Jika yang diserang adalah Manusia
        {
            currentAttackPhase = AttackPhase.AwaitingTangkisPlayer;
            string message = isCounter ? $"AI melakukan Serang Balik dengan kekuatan {value}! Tangkis serangan ini?" : $"Anda diserang dengan kekuatan {value}! Tangkis serangan ini?";
            uiManager.ShowMessage(message, 0f);

            // Sembunyikan panel kartu & info serangan agar fokus ke pilihan
            if (uiManager.KartuManusiaPanel != null) uiManager.KartuManusiaPanel.SetActive(false);
            uiManager.HideAttackStrength();

            uiManager.AksiTangkisPanel.SetActive(true); // Tampilkan pilihan "Tangkis Ya/Tidak"
        }
    }

    // Dipanggil saat pemain menekan "Ya" pada panel Tangkis
    public void OnPlayerTangkisYes()
    {
        if (currentAttackPhase != AttackPhase.AwaitingTangkisPlayer) return;
        currentAttackPhase = AttackPhase.PlayerSelectingParryCards; // Ubah state
        uiManager.AksiTangkisPanel.SetActive(false);
        uiManager.KonfirmasiTangkisButton.gameObject.SetActive(true); // Tampilkan tombol konfirmasi
        // Atur listener tombol konfirmasi
        uiManager.KonfirmasiTangkisButton.onClick.RemoveAllListeners();
        uiManager.KonfirmasiTangkisButton.onClick.AddListener(OnPlayerConfirmTangkis);
        
        uiManager.ShowMessage($"Pilih kartu dengan total nilai {attackValue}, lalu tekan Konfirmasi.", 0f);
        if (uiManager.KartuManusiaPanel != null) uiManager.KartuManusiaPanel.SetActive(true); // Tampilkan kartu
        uiManager.SetPlayerHandInteractable(true); // Kartu bisa diklik
        
        // Update UI untuk menampilkan total tangkisan (awal 0)
        uiManager.UpdateSelectedParryTotal(0, attackValue);
    }
    
    // Dipanggil saat pemain menekan "Tidak" pada panel Tangkis
    public void OnPlayerTangkisNo()
    {
        if (currentAttackPhase != AttackPhase.AwaitingTangkisPlayer) return;
        uiManager.HideAllPlayerPanels();
        currentAttackPhase = AttackPhase.None;
        StartCoroutine(RoundOverCoroutine(attacker)); // Serangan berhasil, ronde selesai
    }
    
    // Dipanggil dari OnCardSlotClicked saat state = PlayerSelectingParryCards
    private void HandleParrySelection(Card clickedCard, SistemKartu slotController)
    {
        var cardEntry = new KeyValuePair<Card, SistemKartu>(clickedCard, slotController);
        
        // Cek apakah kartu ini sudah dipilih
        if (selectedParryCards.Any(p => p.Value == slotController))
        {
            // Jika sudah, batalkan pilihan
            selectedParryCards.RemoveAll(p => p.Value == slotController);
            slotController.ToggleSelection(false); // Kembalikan warna
        }
        else
        {
            // Jika belum, tambahkan ke daftar
            selectedParryCards.Add(cardEntry);
            slotController.ToggleSelection(true); // Ubah warna jadi kuning
        }
        
        // Hitung total nilai dari kartu yang dipilih
        int currentParryTotal = selectedParryCards.Sum(entry => entry.Key.value);
        
        // Update UI total tangkisan
        uiManager.UpdateSelectedParryTotal(currentParryTotal, attackValue);
    }

    // Dipanggil saat tombol "Konfirmasi Tangkis" ditekan
    public void OnPlayerConfirmTangkis()
    {
        uiManager.KonfirmasiTangkisButton.gameObject.SetActive(false);
        uiManager.SetPlayerHandInteractable(false);
        uiManager.HideMessage();
        
        int totalParryValue = selectedParryCards.Sum(entry => entry.Key.value);
        bool hasEnoughValue = totalParryValue == attackValue; // Cek nilai pas
        // Cek jika sisa kartu >= 1 (untuk serang balik)
        bool hasRemainingCardForCounter = manusia.hand.Count - selectedParryCards.Count >= 1;

        if (hasEnoughValue) // Jika nilai tangkisan pas
        {
            if (isCurrentAttackACounter) // Jika yang ditangkis adalah Serang Balik
            {
                // Berhasil, ronde seri, giliran berakhir
                uiManager.ShowMessage("Serang balik berhasil ditangkis!", 2.5f);
                foreach (var entry in selectedParryCards) manusia.hand.Remove(entry.Key); // Buang kartu
                uiManager.UpdatePlayerHandUI(manusia);
                EndTurn(); // Akhiri giliran
            }
            else if (hasRemainingCardForCounter) // Jika yang ditangkis adalah serangan biasa & punya kartu sisa
            {
                // Berhasil, siapkan Serang Balik
                uiManager.ShowMessage("Tangkisan berhasil! Siapkan Serang Balik.", 1.5f);
                foreach (var entry in selectedParryCards) manusia.hand.Remove(entry.Key); // Buang kartu
                uiManager.UpdatePlayerHandUI(manusia);
                StartCoroutine(ExecutePlayerSerangBalikCoroutine()); // Mulai fase serang balik
            }
            else
            {
                // Nilai pas, TAPI tidak punya kartu sisa untuk serang balik
                uiManager.ShowMessage("Tangkisan Gagal! Anda harus menyisakan minimal 1 kartu untuk bisa Serang Balik.", 3f);
                StartCoroutine(RoundOverCoroutine(attacker)); // Ronde berakhir
            }
        }
        else // Jika nilai tangkisan tidak pas
        {
            // Gagal
            uiManager.ShowTangkisanGagalMessage(totalParryValue, attackValue, 3f);
            StartCoroutine(RoundOverCoroutine(attacker)); // Ronde berakhir
        }

        // Bersihkan daftar kartu tangkisan
        foreach (var entry in selectedParryCards)
        {
            entry.Value.ToggleSelection(false); // Kembalikan warna
        }
        selectedParryCards.Clear();
        currentAttackPhase = AttackPhase.None; // Reset state
    }
    
    // Memulai fase Serang Balik untuk pemain
    private IEnumerator ExecutePlayerSerangBalikCoroutine()
    {
        yield return new WaitForSeconds(1.5f); // Jeda dramatis
        isPlayerInSerangBalikMode = true; // Set state
        uiManager.ShowMessage("Pilih satu kartu untuk melakukan Serang Balik.", 0f);
        if (uiManager.KartuManusiaPanel != null) uiManager.KartuManusiaPanel.SetActive(true);
        uiManager.SetPlayerHandInteractable(true); // Kartu bisa diklik
    }
    
    // --- LOGIKA AI ---

    // Logika utama giliran AI
    private IEnumerator ExecuteAITurnCoroutine()
    {
        CameraManager.Instance.SwitchToOverview();
        uiManager.EnterFokusMode("Giliran AI..."); // Sembunyikan UI pemain
        yield return new WaitForSeconds(1.5f);

        if (ai.hand.Count == 0) { EndTurn(); yield break; } // Jika AI tidak punya kartu

        int distance = ai.position - manusia.position; // Hitung jarak
        // Cek semua kemungkinan aksi
        bool canAttack = ai.hand.Any(card => card.value == distance);
        bool canMoveForward = ai.hand.Any(card => ai.position - card.value > manusia.position);
        bool canMoveBackward = ai.hand.Any(card => ai.position + card.value <= 23);

        // Prioritas Aksi AI: Serang > Maju > Mundur
        if (canAttack)
        {
            Card attackCard = ai.hand.First(card => card.value == distance); // Ambil kartu
            uiManager.ShowMessage($"AI menyerang dengan kekuatan {attackCard.value}.", 2f);
            yield return new WaitForSeconds(1f);
            ai.hand.Remove(attackCard); // Buang kartu
            InitiateAttack(ai, manusia, attackCard.value, isCounter: false); // Mulai serangan
        }
        else if (canMoveForward)
        {
            // Ambil semua kartu yang valid untuk maju
            var validCards = ai.hand.Where(card => ai.position - card.value > manusia.position).ToList();
            Card chosenCard = validCards[Random.Range(0, validCards.Count)]; // Pilih acak
            ai.position -= chosenCard.value; // Update posisi
            uiManager.ShowMessage("AI melangkah maju.", 2f);
            MovePawnVisual(ai, ai.position); // Pindahkan pion
            ai.hand.Remove(chosenCard); // Buang kartu
            yield return new WaitForSeconds(1.5f);
            EndTurn(); // Akhiri giliran
        }
        else if (canMoveBackward)
        {
            // Ambil semua kartu yang valid untuk mundur
            var validCards = ai.hand.Where(card => ai.position + card.value <= 23).ToList();
            Card chosenCard = validCards[Random.Range(0, validCards.Count)]; // Pilih acak
            ai.position += chosenCard.value; // Update posisi
            uiManager.ShowMessage("AI melangkah mundur.", 2f);
            MovePawnVisual(ai, ai.position);
            ai.hand.Remove(chosenCard);
            yield return new WaitForSeconds(1.5f);
            EndTurn();
        }
        else
        {
            // Jika AI tidak bisa melakukan apa-apa
            StartCoroutine(ExecuteTieBreakerCoroutine());
        }
    }
    
    // Logika AI untuk memutuskan tangkisan
    private IEnumerator DecideAITangkisCoroutine()
    {
        CameraManager.Instance.SwitchToParry();
        yield return new WaitForSeconds(1.5f);
        
        // Cek apakah AI bisa menangkis
        // 'mustHaveCardLeft' = true jika ini BUKAN serang balik (AI harus sisakan kartu)
        List<Card> parryCombination = FindParryCombination(attackValue, ai.hand, !isCurrentAttackACounter);

        if (parryCombination != null) // Jika AI menemukan kombinasi
        {
            uiManager.ShowMessage($"AI berhasil menangkis serangan Anda.", 2.5f);
            yield return new WaitForSeconds(2f);
            foreach (var card in parryCombination) ai.hand.Remove(card); // Buang kartu

            if (isCurrentAttackACounter) // Jika ini adalah serang balik pemain
            {
                EndTurn(); // Giliran berakhir (seri)
            }
            else // Jika ini serangan biasa
            {
                currentAttackPhase = AttackPhase.AwaitingSerangBalik;
                StartCoroutine(ExecuteAISerangBalikCoroutine()); // AI akan serang balik
            }
        }
        else // Jika AI tidak bisa menangkis
        {
            StartCoroutine(RoundOverCoroutine(attacker)); // Pemain (attacker) menang
        }
    }
    
    // Logika AI untuk melakukan Serang Balik
    private IEnumerator ExecuteAISerangBalikCoroutine()
    {
        if (ai.hand.Count == 0) // Cek jika AI punya kartu
        {
            EndTurn();
            yield break;
        }
        
        Card counterCard = ai.hand[Random.Range(0, ai.hand.Count)]; // Pilih kartu acak
        uiManager.ShowMessage($"AI melakukan Serang Balik dengan kekuatan {counterCard.value}!", 2f);
        yield return new WaitForSeconds(1.5f);
        ai.hand.Remove(counterCard); // Buang kartu
        InitiateAttack(ai, manusia, counterCard.value, true); // Mulai serangan balik
    }
    
    // Algoritma (rekursif) untuk mencari kombinasi kartu (Subset Sum Problem)
    private List<Card> FindParryCombination(int target, List<Card> hand, bool mustHaveCardLeft)
    {
        // Fungsi helper rekursif
        List<Card> FindSubsetSum(int currentTarget, List<Card> currentHand, List<Card> currentCombination)
        {
            if (currentTarget == 0) // Jika totalnya pas
            {
                // Cek apakah AI harus sisakan kartu
                if (mustHaveCardLeft && hand.Count - currentCombination.Count < 1) return null;
                return currentCombination; // Berhasil
            }
            if (currentTarget < 0 || currentHand.Count == 0) return null; // Gagal
            
            Card head = currentHand[0]; // Ambil kartu pertama
            List<Card> tail = currentHand.GetRange(1, currentHand.Count - 1); // Sisa kartu
            
            // Coba 1: Gunakan kartu 'head'
            var withHead = new List<Card>(currentCombination) { head };
            var resultWith = FindSubsetSum(currentTarget - head.value, tail, withHead);
            if (resultWith != null) return resultWith;
            
            // Coba 2: Jangan gunakan kartu 'head'
            var resultWithout = FindSubsetSum(currentTarget, tail, currentCombination);
            return resultWithout;
        }
        // Mulai pencarian
        return FindSubsetSum(target, new List<Card>(hand), new List<Card>());
    }

    // --- AKHIR RONDE / GAME ---

    // Dipanggil saat ronde berakhir
    private IEnumerator RoundOverCoroutine(Player winner)
    {
        CameraManager.Instance.SwitchToOverview();
        uiManager.HideAllPlayerPanels();
        uiManager.HideAttackStrength();
        uiManager.HideMessage();
        uiManager.ShowMessage($"{winner.playerName} memenangkan ronde!", 2.5f);
        winner.score++; // Tambah skor
        uiManager.UpdateScoreUI(winner); // Update UI skor
        yield return new WaitForSeconds(2.5f);

        if (winner.score >= 5) // Cek jika game berakhir
        {
            isGameOver = true;
            yield return new WaitForSeconds(0.5f);
            uiManager.ShowKemenanganPanel(winner == manusia); // Tampilkan panel kemenangan
        }
        else
        {
            StartRound(); // Mulai ronde baru
        }
    }
    
    // Dipanggil jika tidak ada aksi tersisa
    private IEnumerator ExecuteTieBreakerCoroutine()
    {
        uiManager.EnterFokusMode("Tidak ada aksi tersisa! TIE BREAKER diaktifkan.");
        yield return new WaitForSeconds(2.5f);

        // Hitung total nilai kartu di tangan
        int totalManusia = manusia.hand.Sum(card => card.value);
        int totalAI = ai.hand.Sum(card => card.value);

        Player winner = null;
        if (totalManusia > totalAI) winner = manusia;
        else if (totalAI > totalManusia) winner = ai; // Bug fix: harusnya totalAI > totalManusia

        if (winner != null)
        {
            StartCoroutine(RoundOverCoroutine(winner)); // Tentukan pemenang
        }
        else
        {
            // Jika seri
            uiManager.ShowMessage("Hasil Tie Breaker seri!", 2.5f);
            yield return new WaitForSeconds(2.5f);
            StartRound(); // Ulangi ronde
        }
    }

    // --- FUNGSI INPUT LAINNYA ---

    // Dipanggil saat tombol "Ya" di panel Perkuat ditekan
    public void OnPerkuatYesButtonPressed()
    {
        if (uiManager.PerkuatSeranganPanel != null) uiManager.PerkuatSeranganPanel.SetActive(false);
        isPlayerStrengtheningAttack = true; // Set state
        uiManager.ShowMessage("Pilih satu kartu tambahan untuk memperkuat serangan.", 0f);
        if (uiManager.KartuManusiaPanel != null) uiManager.KartuManusiaPanel.SetActive(true);
        uiManager.SetPlayerHandInteractable(true);
    }
    
    // Dipanggil saat tombol "Tidak" di panel Perkuat ditekan
    public void OnPerkuatNoButtonPressed()
    {
        uiManager.HideAllPlayerPanels();
        // Langsung serang dengan kartu awal
        InitiateAttack(manusia, ai, initialAttackCard.value, false);
    }
    
    // Dipanggil saat tombol "Ya" di panel Sergap ditekan
    public void OnSergapYesButtonPressed()
    {
        if (uiManager.AksiSergapPanel != null) uiManager.AksiSergapPanel.SetActive(false);
        isPlayerInSergapMode = true; // Set state
        uiManager.ShowMessage("Pilih kartu untuk melakukan Sergap.", 0f);
        if (uiManager.KartuManusiaPanel != null) uiManager.KartuManusiaPanel.SetActive(true);
        uiManager.SetPlayerHandInteractable(true);
    }

    // Dipanggil saat tombol "Tidak" di panel Sergap ditekan
    public void OnSergapNoButtonPressed()
    {
        uiManager.HideAllPlayerPanels();
        EndTurn(); // Lewati kesempatan sergap, akhiri giliran
    }
    
    // --- FUNGSI UTILITAS ---

    // Mengecek aksi yang valid untuk pemain dan mengupdate tombol
    private void CheckAvailablePlayerActions()
    {
        // Cek bisa gerak: (maju & tidak nabrak) ATAU (mundur & tidak keluar papan)
        bool canMove = manusia.hand.Any(card => (manusia.position + card.value < ai.position) || (manusia.position - card.value >= 1));
        int distance = ai.position - manusia.position;
        // Cek bisa serang: punya kartu = jarak
        bool canAttack = manusia.hand.Any(card => card.value == distance);
        
        uiManager.UpdateActionButtons(canMove, canAttack); // Update tombol

        if (!canMove && !canAttack) // Jika tidak bisa apa-apa
        {
            StartCoroutine(ExecuteTieBreakerCoroutine()); // Tie breaker
        }
    }
    
    // Mengecek arah gerak yang valid
    private void CheckAvailableMoveDirections()
    {
        bool canMoveForward = manusia.hand.Any(card => manusia.position + card.value < ai.position);
        bool canMoveBackward = manusia.hand.Any(card => manusia.position - card.value >= 1);
        uiManager.UpdateMoveDirectionButtons(canMoveForward, canMoveBackward); // Update tombol
    }

    // Memindahkan visual GameObject pion
    private void MovePawnVisual(Player player, int targetPosition)
    {
        GameObject pawn = player.isAI ? pionAI : pionManusia; // Tentukan pion
        if (targetPosition > 0 && targetPosition <= petakPapan.Length)
        {
            // Ambil transform petak tujuan dari array
            Transform targetPetak = petakPapan[targetPosition - 1]; // -1 karena array index
            // Pindahkan pion ke posisi petak (dengan offset Y agar di atas)
            pawn.transform.position = targetPetak.position + new Vector3(0, 75f, 0);
        }
    }

    // Mengisi ulang tangan pemain hingga 5 kartu
    private void RefillHand(Player player)
    {
        while (player.hand.Count < 5)
        {
            Card newCard = DrawCardFromDeck(); // Ambil kartu dari deck
            if (newCard == null) // Jika deck habis
            {
                return; // Berhenti
            }
            player.hand.Add(newCard); // Tambahkan ke tangan
        }
        if (!player.isAI) uiManager.UpdatePlayerHandUI(player); // Update UI
        uiManager.UpdateMainDeckUI(deck.Count); // Update UI sisa deck
    }

    // Membuat isi deck (5 kartu untuk tiap nilai 1-5)
    private void CreateDeck()
    {
        deck.Clear();
        for (int value = 1; value <= 5; value++)
        {
            for (int i = 0; i < 5; i++) deck.Add(new Card(value)); // Total 25 kartu
        }
    }

    // Mengocok deck (Fisher-Yates shuffle)
    private void ShuffleDeck()
    {
        System.Random rng = new System.Random();
        int n = deck.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            Card value = deck[k];
            deck[k] = deck[n];
            deck[n] = value;
        }
    }

    // Mengambil satu kartu dari atas deck
    private Card DrawCardFromDeck()
    {
        if (deck.Count == 0) return null; // Deck habis
        Card drawn = deck[0];
        deck.RemoveAt(0);
        return drawn;
    }

    // --- FUNGSI UNTUK PANEL KEMENANGAN ---

    // Dipanggil tombol "Main Lagi"
    public void PlayAgain()
    {
        manusia.score = 0;
        ai.score = 0;
        Start(); // Memulai ulang game
    }

    // Dipanggil tombol "Menu Utama"
    public void GoToMainMenu()
    {
        SceneManager.LoadScene("MainMenu"); // Ganti scene
    }

    // --- FUNGSI DEBUG KAMERA (TIDAK TERPAKAI DI LOGIKA) ---
    void OnPlayerTurnStart() => CameraManager.Instance.SwitchToPlayerTurn();
    void OnPlayerMove() => CameraManager.Instance.SwitchToFollowMove();
    void OnPlayerAttack() => CameraManager.Instance.SwitchToAttack();
    void OnEnemyParry() => CameraManager.Instance.SwitchToParry();
    void OnRoundEnd() => CameraManager.Instance.SwitchToOverview();
}