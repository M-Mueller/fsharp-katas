module Todo.App

open System
open Feliz
open Feliz.svg
open Elmish
open Elmish.React
open Elmish.HMR

type Todo =
    { id: Guid
      text: string
      isDone: bool }

type EditedTodo =
    { id: Guid
      text: string
      initial: string }

type State =
    { newTodo: string
      todos: Todo list
      editedTodo: EditedTodo option }

let init () =
    { newTodo = ""
      todos = []
      editedTodo = None }

type Msg =
    | SetNewTodo of string
    | AddTodo
    | ToggleTodo of Guid
    | DeleteTodo of Guid
    | EditTodo of Guid
    | SetEditedText of string
    | CancelEditTodo
    | SaveEditTodo

let update (msg: Msg) (state: State) : State =
    match msg with
    | SetNewTodo text -> { state with newTodo = text }
    | AddTodo ->
        let newTodo =
            { id = Guid.NewGuid()
              text = state.newTodo
              isDone = false }

        { state with
              todos = newTodo :: state.todos
              newTodo = "" }
    | ToggleTodo id ->
        let updateTodo (todo: Todo) =
            if todo.id = id then
                { todo with isDone = not todo.isDone }
            else
                todo

        { state with
              todos = List.map updateTodo state.todos }
    | DeleteTodo id ->
        { state with
              todos =
                  state.todos
                  |> List.filter (fun todo -> todo.id <> id) }
    | EditTodo id ->
        { state with
              editedTodo =
                  state.todos
                  |> List.tryFind (fun todo -> todo.id = id)
                  |> Option.map
                      (fun todo ->
                          { id = todo.id
                            text = todo.text
                            initial = todo.text }) }
    | SetEditedText text ->
        match state.editedTodo with
        | Some editedTodo ->
            { state with
                  editedTodo = Some { editedTodo with text = text } }
        | None -> state
    | CancelEditTodo -> { state with editedTodo = None }
    | SaveEditTodo ->
        match state.editedTodo with
        | None -> state
        | Some editedTodo when editedTodo.text = "" -> state
        | Some editedTodo ->
            let updateTodo (todo: Todo) =
                if todo.id = editedTodo.id then
                    { todo with text = editedTodo.text }
                else
                    todo

            { state with
                  editedTodo = None
                  todos = List.map updateTodo state.todos }


let renderInput (currentText: string) (dispatch: Msg -> unit) =
    Html.div [
        prop.className "space-x"
        prop.style [ style.display.flex ]
        prop.children [
            Html.input [
                prop.valueOrDefault currentText
                prop.onChange (fun s -> dispatch (SetNewTodo s))
            ]
            Html.button [
                prop.style [ style.width.minContent ]
                prop.disabled (String.IsNullOrWhiteSpace currentText)
                prop.children [ Icons.plus ]
                prop.onClick (fun _ -> dispatch AddTodo)
            ]
        ]
    ]


let renderTodo (todo: Todo) (dispatch: Msg -> unit) =
    Html.article [
        prop.key todo.id
        prop.className [
            "space-x"
            if todo.isDone then "dimmed"
        ]
        prop.style [
            style.display.flex
            style.flexDirection.row
        ]
        prop.children [
            Html.span [
                prop.className [
                    if todo.isDone then "line-through"
                ]
                prop.style [
                    style.flexGrow 1
                    style.alignSelf.center
                    style.wordBreak.breakWord
                ]
                prop.text todo.text
                prop.onClick (fun _ -> dispatch (ToggleTodo todo.id))
            ]
            if todo.isDone then
                Html.button [
                    prop.className [ "outline"; "danger" ]
                    prop.style [
                        style.alignSelf.center
                        style.width.minContent
                        style.marginBottom 0
                    ]
                    prop.onClick (fun _ -> dispatch (DeleteTodo todo.id))
                    prop.children [ Icons.trash ]
                ]
            else
                Html.button [
                    prop.className [ "outline" ]
                    prop.style [
                        style.alignSelf.center
                        style.width.minContent
                        style.marginBottom 0
                    ]
                    prop.onClick (fun _ -> dispatch (EditTodo todo.id))
                    prop.children [ Icons.edit ]
                ]
        ]
    ]

let renderEditedTodo (todo: EditedTodo) (dispatch: Msg -> unit) =
    Html.article [
        prop.className "space-x"
        prop.style [
            style.display.flex
            style.flexDirection.row
        ]
        prop.children [
            Html.input [
                prop.style [
                    style.flexGrow 1
                    style.alignSelf.center
                    style.wordBreak.breakWord
                    style.marginBottom 0
                ]
                prop.valueOrDefault todo.text
                prop.onChange (fun s -> dispatch (SetEditedText s))
            ]
            Html.button [
                prop.style [
                    style.alignSelf.center
                    style.width.minContent
                    style.marginBottom 0
                ]
                prop.disabled (
                    todo.text = todo.initial
                    || String.IsNullOrWhiteSpace todo.text
                )
                prop.onClick (fun _ -> dispatch (SaveEditTodo))
                prop.children [ Icons.save ]
            ]
            Html.button [
                prop.className "outline"
                prop.style [
                    style.alignSelf.center
                    style.width.minContent
                    style.marginBottom 0
                ]
                prop.onClick (fun _ -> dispatch (CancelEditTodo))
                prop.children [ Icons.x ]
            ]
        ]
    ]

let renderTodos (state: State) (dispatch: Msg -> unit) =
    // render finished todos at the end
    let doneTodos, remainingTodos =
        List.partition (fun todo -> todo.isDone) state.todos

    Html.div [
        for todo in remainingTodos ->
            match state.editedTodo with
            | Some editedTodo when editedTodo.id = todo.id -> renderEditedTodo editedTodo dispatch
            | _ -> renderTodo todo dispatch
        for todo in doneTodos -> renderTodo todo dispatch
    ]

let render (state: State) (dispatch: Msg -> unit) =
    Html.div [
        prop.className "container"
        prop.children [
            Html.h1 "To-Do List"
            renderInput state.newTodo dispatch
            renderTodos state dispatch
        ]
    ]


Program.mkSimple init update render
|> Program.withReactSynchronous "elmish-app"
|> Program.run
