!\[addbook](../images/bookadd.png)



```plantuml



@startuml

title Добавление книги в фонд



actor "Администратор" as Admin

participant "ABC Library System" as Facade

database "Catalog Storage" as CatalogStore

participant "Cabinet (Composite)" as Cabinet



Admin -> Facade : addBook(bookId, title, author)

activate Facade

Facade -> CatalogStore : addBook(book)

activate CatalogStore

CatalogStore -> CatalogStore : checkIdUnique()



alt Книга уже существует

&#x20;   CatalogStore --> Facade : error("Duplicate")

&#x20;   Facade -> Admin : showError("Книга уже есть")

else ID уникален

&#x20;   CatalogStore --> Facade : ok

&#x20;   Facade -> Admin : showSuccess("Книга добавлена")

end

deactivate CatalogStore

deactivate Facade



Admin -> Facade : addCopy(bookId, invNum)

activate Facade

Facade -> CatalogStore : addCopy(bookId, invNum)

activate CatalogStore

CatalogStore -> Cabinet : findFreeSlot()

activate Cabinet

Cabinet --> CatalogStore : slot

deactivate Cabinet



alt Нет свободных мест

&#x20;   CatalogStore --> Facade : error("No space")

&#x20;   Facade -> Admin : showError("Хранилище заполнено")

else Место найдено

&#x20;   CatalogStore -> Cabinet : slot.place(copy)

&#x20;   activate Cabinet

&#x20;   Cabinet --> CatalogStore : ok

&#x20;   deactivate Cabinet

&#x20;   

&#x20;   CatalogStore --> Facade : ok

&#x20;   Facade -> Admin : showSuccess("Экземпляр добавлен")

end

deactivate CatalogStore

deactivate Facade

@enduml

