

!\[usecase](https://raw.githubusercontent.com/penamarth/bsbo-11-24\_team6\_var2\_bibl/main/images/usecase.png)



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



' Связи Мученика

Mychennik --> UC\_Search

Mychennik --> UC\_Reserve



' Связи Библиотекаря

Librarian --> UC\_Search

Librarian --> UC\_Issue

Librarian --> UC\_Return



' Связи Администратора

Admin --> UC\_Manage

@enduml

```

