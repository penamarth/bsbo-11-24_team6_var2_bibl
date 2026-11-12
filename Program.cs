using System;
using System.Collections.Generic;
using System.Linq;

// Проект: ABC alesandriiskaya biblioteka cgorela
namespace ABC_Library_System
{
    // =========================================================
    // 1. ENUMS & VALUE OBJECTS
    // =========================================================
    public enum CopyStatus { Available, OnLoan, Reserved, WrittenOff }
    public enum ReservationStatus { Active, Fulfilled, Cancelled, Expired }
    public enum MychennikStatus { Active, Blocked }

    // =========================================================
    // 2. DOMAIN MODELS (Модель предметной области)
    // =========================================================
    public class Book
    {
        public string BookId { get; } 
        public string Title { get; }
        public string Author { get; }
        public int Year { get; }
        public string Description { get; }

        public Book(string bookId, string title, string author, int year, string description = "")
        {
            BookId = bookId ?? throw new ArgumentNullException(nameof(bookId));
            Title = title ?? throw new ArgumentNullException(nameof(title));
            Author = author;
            Year = year;
            Description = description;
        }

        public bool Matches(string title, string author)
        {
            if (!string.IsNullOrEmpty(title) && !Title.Contains(title, StringComparison.OrdinalIgnoreCase)) return false;
            if (!string.IsNullOrEmpty(author) && !Author.Contains(author, StringComparison.OrdinalIgnoreCase)) return false;
            return true;
        }
    }

    public class BookCopy
    {
        public string InventoryNumber { get; }
        public Book Book { get; }
        public CopyStatus Status { get; internal set; }
        public string LocationPath { get; internal set; }
        public string HolderId { get; internal set; }
        public DateTime? DueDate { get; internal set; }

        public BookCopy(string inventoryNumber, Book book, string locationPath)
        {
            InventoryNumber = inventoryNumber;
            Book = book;
            LocationPath = locationPath;
            Status = CopyStatus.Available;
        }

        public bool IsAvailable() => Status == CopyStatus.Available;
    }

    public class Mychennik
    {
        public string Id { get; }
        public string FullName { get; }
        public string Phone { get; }
        public string Email { get; }
        public MychennikStatus Status { get; internal set; }

        public Mychennik(string id, string fullName, string phone, string email)
        {
            Id = id;
            FullName = fullName;
            Phone = phone;
            Email = email;
            Status = MychennikStatus.Active;
        }
    }

    public class Loan
    {
        public string Id { get; }
        public string MychennikId { get; } 
        public string InventoryNumber { get; }
        public DateTime IssueDate { get; }
        public DateTime DueDate { get; }
        public DateTime? ReturnDate { get; internal set; }

        public Loan(string id, string mychennikId, string inventoryNumber, DateTime issueDate, DateTime dueDate)
        {
            Id = id;
            MychennikId = mychennikId;
            InventoryNumber = inventoryNumber;
            IssueDate = issueDate;
            DueDate = dueDate;
        }

        public bool IsOverdue(DateTime now) => !ReturnDate.HasValue && now.Date > DueDate.Date;
    }

    public class Reservation
    {
        public string Id { get; }
        public string MychennikId { get; } 
        public string BookId { get; }
        public DateTime CreatedAt { get; }
        public DateTime ExpiryDate { get; }
        public ReservationStatus Status { get; internal set; }

        public Reservation(string id, string mychennikId, string bookId, DateTime createdAt, DateTime expiryDate)
        {
            Id = id;
            MychennikId = mychennikId;
            BookId = bookId;
            CreatedAt = createdAt;
            ExpiryDate = expiryDate;
            Status = ReservationStatus.Active;
        }
    }

    // =========================================================
    // 3. COMPOSITE PATTERN (Физическое хранилище)
    // =========================================================
    public abstract class StorageNode
    {
        public string Name { get; }
        protected StorageNode(string name) => Name = name;
        public abstract Slot FindFreeSlot();
        public abstract IEnumerable<BookCopy> GetAllCopies();
    }

    public class Cabinet : StorageNode
    {
        public List<Shelf> Shelves { get; } = new();
        public Cabinet(string name, int shelfCount, int slotsPerShelf) : base(name)
        {
            for (int i = 0; i < shelfCount; i++)
                Shelves.Add(new Shelf($"{name}/S{i + 1}", slotsPerShelf));
        }
        public override Slot FindFreeSlot() => Shelves.Select(s => s.FindFreeSlot()).FirstOrDefault(s => s != null);
        public override IEnumerable<BookCopy> GetAllCopies() => Shelves.SelectMany(s => s.GetAllCopies());
    }

    public class Shelf : StorageNode
    {
        public List<Slot> Slots { get; } = new();
        public Shelf(string name, int slotCount) : base(name)
        {
            for (int i = 0; i < slotCount; i++)
                Slots.Add(new Slot($"{name}/C{i + 1}"));
        }
        public override Slot FindFreeSlot() => Slots.FirstOrDefault(s => s.IsFree());
        public override IEnumerable<BookCopy> GetAllCopies() => Slots.Where(s => s.Copy != null).Select(s => s.Copy);
    }

    public class Slot : StorageNode
    {
        public BookCopy Copy { get; internal set; }
        public Slot(string name) : base(name) { }
        public bool IsFree() => Copy == null;

        public void Place(BookCopy copy)
        {
            if (!IsFree()) throw new InvalidOperationException($"Ячейка {Name} занята");
            Copy = copy;
            copy.LocationPath = Name;
        }

        public BookCopy Remove()
        {
            var c = Copy;
            Copy = null;
            return c;
        }

        public override Slot FindFreeSlot() => IsFree() ? this : null;
        public override IEnumerable<BookCopy> GetAllCopies()
        {
            if (Copy != null) yield return Copy;
        }
    }

    // =========================================================
    // 4. STORAGES (Репозитории данных)
    // =========================================================
    public class CatalogStorage
    {
        private readonly Dictionary<string, Book> _books = new();
        private readonly Dictionary<string, BookCopy> _copies = new();
        public Cabinet MainCabinet { get; }

        public CatalogStorage(int shelves = 5, int slotsPerShelf = 10)
        {
            MainCabinet = new Cabinet("CAB-1", shelves, slotsPerShelf);
        }

        public void AddBook(Book book)
        {
            if (_books.ContainsKey(book.BookId)) throw new InvalidOperationException("Книга уже существует");
            _books[book.BookId] = book;
        }

        public BookCopy AddCopy(string bookId, string inventoryNumber)
        {
            if (!_books.TryGetValue(bookId, out var book)) throw new InvalidOperationException("Книга не найдена");
            if (_copies.ContainsKey(inventoryNumber)) throw new InvalidOperationException("Экземпляр уже существует");

            var slot = MainCabinet.FindFreeSlot() ?? throw new InvalidOperationException("Нет свободных мест в хранилище");
            var copy = new BookCopy(inventoryNumber, book, slot.Name);
            slot.Place(copy);
            _copies[inventoryNumber] = copy;
            return copy;
        }

        public void WriteOffCopy(string inventoryNumber)
        {
            if (!_copies.TryGetValue(inventoryNumber, out var copy)) throw new InvalidOperationException("Экземпляр не найден");
            if (copy.Status == CopyStatus.OnLoan) throw new InvalidOperationException("Нельзя списать выданный экземпляр");

            copy.Status = CopyStatus.WrittenOff;
        }

        public BookCopy GetCopy(string inventoryNumber) => _copies.GetValueOrDefault(inventoryNumber);

        public BookCopy FindAvailableCopy(string bookId) =>
            _copies.Values.FirstOrDefault(c => c.Book.BookId == bookId && c.IsAvailable());

        public List<Book> SearchBooks(string title, string author) =>
            _books.Values.Where(b => b.Matches(title, author)).ToList();
    }

    public class MychennikStorage
    {
        private readonly Dictionary<string, Mychennik> _mychenniks = new();

        public Mychennik Register(string fullName, string phone, string email)
        {
            var mychennik = new Mychennik(Guid.NewGuid().ToString("N")[..8], fullName, phone, email);
            _mychenniks[mychennik.Id] = mychennik;
            return mychennik;
        }

        public Mychennik GetMychennik(string id) => _mychenniks.GetValueOrDefault(id);

        public void UpdateStatus(string id, MychennikStatus status)
        {
            if (_mychenniks.TryGetValue(id, out var m)) m.Status = status;
        }
    }

    public class CirculationStorage
    {
        private readonly Dictionary<string, Loan> _loans = new();
        private readonly Dictionary<string, List<Reservation>> _reservations = new();
        private const int MaxBooksAllowed = 5;

        public bool CanBorrowMore(string mychennikId, DateTime now)
        {
            var activeLoans = _loans.Values.Where(l => l.MychennikId == mychennikId && !l.ReturnDate.HasValue).ToList();
            if (activeLoans.Count >= MaxBooksAllowed) return false;
            if (activeLoans.Any(l => l.IsOverdue(now))) return false;
            return true;
        }

        public Loan Issue(string mychennikId, string inventoryNumber, DateTime issueDate, DateTime dueDate)
        {
            var loan = new Loan(Guid.NewGuid().ToString("N")[..8], mychennikId, inventoryNumber, issueDate, dueDate);
            _loans[loan.Id] = loan;
            return loan;
        }

        public Loan Return(string inventoryNumber, DateTime returnDate)
        {
            var loan = _loans.Values.FirstOrDefault(l => l.InventoryNumber == inventoryNumber && !l.ReturnDate.HasValue);
            if (loan == null) throw new InvalidOperationException("Активный заём не найден");
            loan.ReturnDate = returnDate;
            return loan;
        }

        public Reservation Reserve(string mychennikId, string bookId, DateTime now, TimeSpan lifetime)
        {
            if (!_reservations.ContainsKey(bookId))
                _reservations[bookId] = new List<Reservation>();

            var res = new Reservation(Guid.NewGuid().ToString("N")[..8], mychennikId, bookId, now, now.Add(lifetime));
            _reservations[bookId].Add(res);
            return res;
        }

        public void CancelReservation(string reservationId)
        {
            foreach (var queue in _reservations.Values)
            {
                var res = queue.FirstOrDefault(r => r.Id == reservationId);
                if (res != null) { res.Status = ReservationStatus.Cancelled; return; }
            }
        }

        public List<Loan> GetActiveLoans() => _loans.Values.Where(l => !l.ReturnDate.HasValue).ToList();

        public List<Reservation> GetActiveReservations(string bookId) =>
            _reservations.TryGetValue(bookId, out var list) ? list.Where(r => r.Status == ReservationStatus.Active).ToList() : new List<Reservation>();
    }

    // =========================================================
    // 5. ADAPTERS (Внешние системы)
    // =========================================================
    public interface INotificationAdapter
    {
        void SendEmail(string email, string subject, string body);
    }

    public class ConsoleNotificationAdapter : INotificationAdapter
    {
        public void SendEmail(string email, string subject, string body)
        {
            Console.WriteLine($"[EMAIL to {email}] {subject}: {body}");
        }
    }

    // =========================================================
    // 6. APPLICATION FACADE (Главный класс приложения)
    // =========================================================
    public class LibraryFacade
    {
        private readonly CatalogStorage _catalog;
        private readonly MychennikStorage _mychenniks; 
        private readonly CirculationStorage _circulation;
        private readonly INotificationAdapter _notifier;

        public LibraryFacade(INotificationAdapter notifier = null)
        {
            _catalog = new CatalogStorage();
            _mychenniks = new MychennikStorage(); 
            _circulation = new CirculationStorage();
            _notifier = notifier ?? new ConsoleNotificationAdapter();
        }

        // --- Управление фондом ---
        public void AddBook(string bookId, string title, string author, int year)
        {
            _catalog.AddBook(new Book(bookId, title, author, year));
        }

        public void AddCopy(string bookId, string inventoryNumber)
        {
            _catalog.AddCopy(bookId, inventoryNumber);
        }

        public void WriteOffCopy(string inventoryNumber)
        {
            _catalog.WriteOffCopy(inventoryNumber);
        }

        // --- Регистрация мучеников ---
        public Mychennik RegisterMychennik(string fullName, string phone, string email)
        {
            return _mychenniks.Register(fullName, phone, email);
        }

        // --- Поиск ---
        public List<Book> SearchBooks(string title, string author)
        {
            return _catalog.SearchBooks(title, author);
        }

        // --- Выдача книги ---
        public bool StartIssueProcess()
        {
            Console.WriteLine("Начало процесса выдачи. Ожидание сканирования QR-кода мученика...");
            return true;
        }

        public Mychennik ScanMychennikQR(string qrCode)
        {
            var mychennik = _mychenniks.GetMychennik(qrCode);
            if (mychennik == null) throw new InvalidOperationException("Мученик не найден");
            if (mychennik.Status == MychennikStatus.Blocked) throw new InvalidOperationException("Мученик заблокирован");
            return mychennik;
        }

        public bool CheckMychennikEligibility(string mychennikId)
        {
            if (!_circulation.CanBorrowMore(mychennikId, DateTime.Now))
                throw new InvalidOperationException("Мученик не может брать книги (лимит или просрочка)");
            return true;
        }

        public void RequestBookScan() => Console.WriteLine("Ожидание сканирования штрихкода экземпляра...");

        public BookCopy ScanBook(string inventoryNumber)
        {
            var copy = _catalog.GetCopy(inventoryNumber);
            if (copy == null) throw new InvalidOperationException("Экземпляр не найден");
            if (!copy.IsAvailable()) throw new InvalidOperationException("Экземпляр недоступен");
            return copy;
        }

        public Loan ConfirmIssue(string mychennikId, string inventoryNumber, int daysToReturn)
        {
            var copy = _catalog.GetCopy(inventoryNumber);
            var mychennik = _mychenniks.GetMychennik(mychennikId);

            var dueDate = DateTime.Now.AddDays(daysToReturn);
            var loan = _circulation.Issue(mychennikId, inventoryNumber, DateTime.Now, dueDate);

            copy.Status = CopyStatus.OnLoan;
            copy.HolderId = mychennikId;
            copy.DueDate = dueDate;

            _notifier.SendEmail(mychennik.Email, "Книга выдана", $"Вы взяли книгу до {dueDate:dd.MM.yyyy}");

            Console.WriteLine($"Книга {inventoryNumber} выдана мученику {mychennik.FullName}. Срок: {dueDate:dd.MM.yyyy}");
            return loan;
        }

        // --- Возврат книги ---
        public Loan ReturnBook(string inventoryNumber)
        {
            var copy = _catalog.GetCopy(inventoryNumber) ?? throw new InvalidOperationException("Экземпляр не найден");
            var loan = _circulation.Return(inventoryNumber, DateTime.Now);

            copy.Status = CopyStatus.Available;
            copy.HolderId = null;
            copy.DueDate = null;

            var reservations = _circulation.GetActiveReservations(copy.Book.BookId);
            var nextInLine = reservations.FirstOrDefault();
            if (nextInLine != null)
            {
                var mychennik = _mychenniks.GetMychennik(nextInLine.MychennikId);
                _notifier.SendEmail(mychennik.Email, "Книга доступна",
                    $"Книга {copy.Book.Title} ожидает вас на абонементе до {nextInLine.ExpiryDate:dd.MM.yyyy}");
                nextInLine.Status = ReservationStatus.Fulfilled;
            }

            return loan;
        }

        // --- Бронирование ---
        public Reservation ReserveBook(string mychennikId, string bookId)
        {
            if (_mychenniks.GetMychennik(mychennikId) == null) throw new InvalidOperationException("Мученик не найден");
            return _circulation.Reserve(mychennikId, bookId, DateTime.Now, TimeSpan.FromDays(3));
        }

        public void CancelReservation(string reservationId)
        {
            _circulation.CancelReservation(reservationId);
        }

        // --- Аналитика ---
        public void PrintActiveLoans()
        {
            var loans = _circulation.GetActiveLoans();
            Console.WriteLine($"\nАктивные займы ({loans.Count}):");
            foreach (var l in loans)
            {
                Console.WriteLine($"  Экземпляр: {l.InventoryNumber}, Мученик: {l.MychennikId}, Срок: {l.DueDate:dd.MM.yyyy}");
            }
        }
    }

    // =========================================================
    // 7. DEMO / PROGRAM
    // =========================================================
    internal static class Program
    {
        private static void Main()
        {
            var facade = new LibraryFacade();

            Console.WriteLine("=== ABC alesandriiskaya biblioteka cgorela ===");

            // 1. Инициализация фонда
            facade.AddBook("B-001", "Мастер и Маргарита", "Булгаков", 1967);
            facade.AddBook("B-002", "Преступление и наказание", "Достоевский", 1866);

            facade.AddCopy("B-001", "INV-001");
            facade.AddCopy("B-001", "INV-002");
            facade.AddCopy("B-002", "INV-003");

            // 2. Регистрация мучеников
            var anna = facade.RegisterMychennik("Анна Петрова", "+7-916-123", "anna@mail.ru");
            var sergey = facade.RegisterMychennik("Сергей Иванов", "+7-903-987", "sergey@mail.ru");

            // 3. Сценарий выдачи
            Console.WriteLine("\n--- Выдача книги ---");
            facade.StartIssueProcess();
            facade.ScanMychennikQR(anna.Id);
            facade.CheckMychennikEligibility(anna.Id);
            facade.RequestBookScan();
            facade.ScanBook("INV-001");
            facade.ConfirmIssue(anna.Id, "INV-001", 14);

            // 4. Попытка выдать тот же экземпляр
            Console.WriteLine("\n--- Попытка выдать занятую книгу ---");
            try
            {
                facade.CheckMychennikEligibility(sergey.Id);
                facade.ScanBook("INV-001"); // Ошибка!
            }
            catch (Exception ex) { Console.WriteLine($"Ожидаемо: {ex.Message}"); }

            // 5. Бронирование
            Console.WriteLine("\n--- Бронирование ---");
            var res = facade.ReserveBook(sergey.Id, "B-001");
            Console.WriteLine($"Создана бронь {res.Id} для {sergey.FullName}");

            // 6. Возврат книги (уведомит следующего в очереди)
            Console.WriteLine("\n--- Возврат книги ---");
            facade.ReturnBook("INV-001");

            // 7. Списание
            Console.WriteLine("\n--- Списание ---");
            facade.WriteOffCopy("INV-003");
            Console.WriteLine("INV-003 списан.");

            // 8. Аналитика
            facade.PrintActiveLoans();
        }
    }
}