module NuPopulator.FSharp.Generate

open Fantomas.Core
open Fantomas.Core.SyntaxOak

let zeroRange = Fantomas.FCS.Text.Range.Zero
let stn text = SingleTextNode(text, zeroRange)
let moduleKeyword = MultipleTextsNode([ stn "module" ], zeroRange)
let letKeyword = MultipleTextsNode([ stn "let" ], zeroRange)
let memberKeyword = MultipleTextsNode([ stn "member" ], zeroRange)
let namespaceKeyword = MultipleTextsNode([ stn "namespace" ], zeroRange)

let unitNode = UnitNode(stn "(", stn ")", zeroRange)
let unitPattern = Pattern.Unit(unitNode)
let unitExpr = Expr.Constant(Constant.Unit(unitNode))
let five = Expr.Constant(Constant.FromText(stn "5"))
let six = Expr.Constant(Constant.FromText(stn "6"))

let join f sep xs =
    match xs with
    | [] -> []
    | [ single ] -> [ f single ]
    | head :: tail ->
        [ yield f head
          for t in tail do
              yield sep
              yield f t ]

let idl (text: string) =
    let parts =
        let parts = text.Split(".") |> List.ofArray
        join (stn >> IdentifierOrDot.Ident) IdentifierOrDot.UnknownDot parts

    IdentListNode(parts, zeroRange)

let mkModuleOrNamespaceNode keyword name decls =
    Oak(
        [],
        [ ModuleOrNamespaceNode(
              Some(ModuleOrNamespaceHeaderNode(None, None, keyword, None, false, Some(idl name), zeroRange)),
              decls,
              zeroRange
          ) ],
        zeroRange
    )

let mkModule name decls =
    mkModuleOrNamespaceNode moduleKeyword name decls

let mkNamespace name decls =
    mkModuleOrNamespaceNode namespaceKeyword name decls

/// Unchecked.defaultof<A>
let uncheckedDefaultofA =
    ExprTypeAppNode(
        ExprOptVarNode(false, idl "Unchecked.defaultof", zeroRange) |> Expr.OptVar,
        stn "<",
        [ Type.LongIdent(idl "A") ],
        stn ">",
        zeroRange
    )
    |> Expr.TypeApp

let mkBinding leadingKeyword name parameters body =
    BindingNode(
        None,
        None,
        leadingKeyword,
        false,
        None,
        None,
        Choice1Of2(idl name),
        None,
        parameters,
        None,
        stn "=",
        body,
        zeroRange
    )

let mkConsumeBinding (referencedProjects: string seq) =
    let parameters =
        let ps =
            referencedProjects
            |> Seq.mapi (fun idx rp ->
                Pattern.Parameter(
                    PatParameterNode(
                        None,
                        Pattern.Named(PatNamedNode(None, stn $"p%i{idx}", zeroRange)),
                        Some(Type.LongIdent(idl $"%s{rp}.A")),
                        zeroRange
                    )
                ))
            |> Seq.toList

        PatParenNode(
            stn "(",
            Pattern.Tuple(PatTupleNode(join Choice1Of2 (Choice2Of2(stn ",")) ps, zeroRange)),
            stn ")",
            zeroRange
        )
        |> Pattern.Paren

    let bodyExpr: Expr =
        if Seq.isEmpty referencedProjects then
            five
        else
            let xs =
                [ 0 .. (Seq.length referencedProjects - 1) ]
                |> List.collect (fun idx ->
                    [ Expr.OptVar(ExprOptVarNode(false, idl $"p{idx}.V", zeroRange))
                      Expr.AppLongIdentAndSingleParenArg(
                          ExprAppLongIdentAndSingleParenArgNode(idl $"p{idx}.F", unitExpr, zeroRange)
                      ) ])

            let plusOperator = stn "+"

            ExprSameInfixAppsNode(xs.Head, xs.Tail |> List.map (fun e -> (plusOperator, e)), zeroRange)
            |> Expr.SameInfixApps

    mkBinding letKeyword "fn" [ parameters ] bodyExpr |> ModuleDecl.TopLevelBinding

let oakToString oak =
    oak |> CodeFormatter.FormatOakAsync |> Async.RunSynchronously

let mkConsumer (namespaceName: string, referencedProjects: string seq) : string =
    mkModule $"%s{namespaceName}.Consumer" [ mkConsumeBinding referencedProjects ]
    |> oakToString

let mkType name : ModuleDecl =
    let ctor = ImplicitConstructorNode(None, None, None, unitPattern, None, zeroRange)
    let property = mkBinding memberKeyword "_.V" [] five |> MemberDefn.Member
    let m = mkBinding memberKeyword "_.F" [ unitPattern ] six |> MemberDefn.Member

    TypeDefnRegularNode(
        TypeNameNode(None, None, stn "type", None, idl name, None, [], Some(ctor), Some(stn "="), None, zeroRange),
        [ property; m ],
        zeroRange
    )
    |> TypeDefn.Regular
    |> ModuleDecl.TypeDefn

let mkProducer (namespaceName: string) =
    mkNamespace
        namespaceName
        [ for char in 'A' .. 'E' do
              mkType (string char) ]
    |> oakToString
