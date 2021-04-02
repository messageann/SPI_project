# Encoder
Проект по дисциплине "Технологии разработки программных приложений".  
Разработчики - Шатохин Никита и Казакова Анна
### Основные функции программы
Цель программы - дать пользователю возможность зашифровать любой вид информации.  
Программа может хранить:  
* логин/пароль  
* текст  
* медиаданные  

Также для шифрования доступен любой тип файла, который просто заменяется на файл с расширением, которое понимает Encoder. Для операционной системы Windows доступно расширение, позволяющее зашифровать файл из контекстного меню файла.
### Шифрование пароля
Для создания требуется заполнить:
1. Имя
2. Описание(опционально)
3. Логин
4. Пароль
5. Уровень защиты
6. Ключи, количество которых определяется уровнем защиты

Далее, для расшифровки пароля требуется ввести ключи (остальные поля доступны без них, кроме логина - его можно шифровать вместе с паролем). После этого пароль копируется в буфер обмена и самоуничтожается оттуда через N-ное время, которое задаётся пользователем в настройках (по умолчанию, 5 секунд).
### Интерфейс программы 
Частично интерактивный интерфейс для ознакомления с принципом работы программы представлен по следующей ссылке:
##### *https://bit.ly/2PQlByE* 
Активны для просмотра папки Files (папка с паролем) и New folder 2 (открытая папка), также доступно взаимодействие с меню и представлен принцип добавления новой папки. В папке Files кликабельны все айтемы и просмотр поля "Key", в New Folder 2 можно посмотреть принцип добавления нового айтема.  
Для просмотра активных объектов нажмите на любую точку и они будут подсвечены.