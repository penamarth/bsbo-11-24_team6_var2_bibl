

![usecase](../images/usecase.png)



```plantuml

@startuml

left to right direction

skinparam packageStyle rect

skinparam actorStyle awesome



actor "Читатель" as Mychennik

actor "Библиотекарь" as Librarian

actor "Администратор" as Admin



rectangle "Информационная система библиотеки" {

&#x20; usecase "Поиск книг в каталоге" as UC\_Search

&#x20; usecase "Бронирование книги" as UC\_Reserve

&#x20; usecase "Выдача книги" as UC\_Issue

&#x20; usecase "Возврат книги" as UC\_Return

&#x20; usecase "Управление фондом\\n(добавление/списание)" as UC\_Manage

}


Mychennik --> UC\_Search

Mychennik --> UC\_Reserve


Librarian --> UC\_Search

Librarian --> UC\_Issue

Librarian --> UC\_Return


Admin --> UC\_Manage

@enduml

```

