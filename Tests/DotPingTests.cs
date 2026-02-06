// using NUnit.Framework;
//
// namespace DotPing.Tests
// {
//     public class PingControllerTests
//     {
//         [SetUp]
//         public void Setup()
//         {
//            
//         }
//
//         [Test]
//         public void Push_ShouldAddNodeWithoutParent()
//         {
//             // Arrange
//             string id = "node1";
//
//             // Act
//             PingController.Push(id);
//
//             // Assert
//             Assert.IsTrue(PingController.IsActive(id));
//         }
//
//         [Test]
//         public void Push_ShouldAddNodeWithParentAndAddToChildrenMap()
//         {
//             // Arrange
//             string parentId = "parent";
//             string childId = "parent/child";
//
//             // Act
//             PingController.Push(childId);
//
//             // Assert
//             Assert.IsTrue(PingController.IsActive(childId));
//             Assert.IsTrue(PingController.IsActive(parentId)); // Parent cũng sẽ được tự động kích hoạt
//         }
//
//         [Test]
//         public void Pop_ShouldRemoveNodeWithoutChildren()
//         {
//             // Arrange
//             string id = "node2";
//             PingController.Push(id);
//
//             // Act
//             PingController.Pop(id);
//
//             // Assert
//             Assert.IsFalse(PingController.IsActive(id));
//         }
//
//         [Test]
//         public void Pop_ShouldRemoveNodeAndUpdateParent()
//         {
//             // Arrange
//             string parentId = "parent";
//             string childId = "parent/child";
//
//             PingController.Push(childId);
//             PingController.Push(parentId);
//
//             // Act
//             PingController.Pop(childId);
//
//             // Assert
//             Assert.IsFalse(PingController.IsActive(childId));
//             Assert.IsTrue(PingController.IsActive(parentId));
//         }
//
//         [Test]
//         public void Pop_ShouldRemoveNodeAndUpdateParent2()
//         {
//             // Arrange
//             string parentId = "parent";
//             string childId = "parent/child";
//
//             PingController.Push(childId);
//
//             // Act
//             PingController.Pop(childId);
//
//             // Assert
//             Assert.IsFalse(PingController.IsActive(childId));
//             Assert.IsFalse(PingController.IsActive(parentId));
//         }
//
//         [Test]
//         public void Pop_ShouldNotDeactivateParentIfOtherChildrenRemain()
//         {
//             // Arrange
//             string parentId = "parent";
//             string childId1 = "parent/child1";
//             string childId2 = "parent/child2";
//
//             PingController.Push(childId1);
//             PingController.Push(childId2);
//
//             // Act
//             PingController.Pop(childId1);
//
//             // Assert
//             Assert.IsFalse(PingController.IsActive(childId1)); // Node con được Pop
//             Assert.IsTrue(PingController.IsActive(parentId)); // Parent vẫn còn một node con hoạt động
//         }
//
//         [Test]
//         public void Pop_WithForceHide_ShouldForceDeactivateNode()
//         {
//             // Arrange
//             string id = "node3";
//             PingController.Push(id);
//
//             // Act
//             PingController.Pop(id, true);
//
//             // Assert
//             Assert.IsFalse(PingController.IsActive(id)); // Bất kể giá trị, node bị force hide sẽ không còn hoạt động
//         }
//
//         [Test]
//         public void Push_ShouldAddNodeAtDepth4_AndActivateAllParents()
//         {
//             // Arrange
//             string level1 = "level1";
//             string level2 = "level1/level2";
//             string level3 = "level1/level2/level3";
//             string level4 = "level1/level2/level3/level4";
//
//             // Act
//             PingController.Push(level4);
//
//             // Assert
//             Assert.IsTrue(PingController.IsActive(level4)); // Node sâu nhất phải được active
//             Assert.IsTrue(PingController.IsActive(level3)); // Parent cấp 3
//             Assert.IsTrue(PingController.IsActive(level2)); // Parent cấp 2
//             Assert.IsTrue(PingController.IsActive(level1)); // Parent cấp 1
//         }
//
//         [Test]
//         public void Pop_AtDepth4_ShouldDisableOnlyRelevantNodes()
//         {
//             // Arrange
//             string level1 = "level1";
//             string level2 = "level1/level2";
//             string level3 = "level1/level2/level3";
//             string level4 = "level1/level2/level3/level4";
//
//             // Push tất cả các node
//             PingController.Push(level4);
//
//             // Act
//             PingController.Pop(level4);
//
//             // Assert
//             Assert.IsFalse(PingController.IsActive(level4)); // Node cấp 4 phải bị vô hiệu hóa
//             Assert.IsFalse(PingController.IsActive(level3)); // Parent cấp 3 không còn con nào, nên bị vô hiệu hóa
//             Assert.IsFalse(PingController.IsActive(level2)); // Parent cấp 2 cũng phải bị vô hiệu hóa
//             Assert.IsFalse(PingController.IsActive(level1)); // Parent cấp 1 bị vô hiệu hóa do không còn con nào
//         }
//
//         [Test]
//         public void Pop_AtDepth3_ShouldNotDisableNodesWithAnotherActiveChild()
//         {
//             // Arrange
//             string level1 = "level1";
//             string level2 = "level1/level2";
//             string child1 = "level1/level2/child1";
//             string child2 = "level1/level2/child2";
//
//             // Push tất cả các node
//             PingController.Push(child1);
//             PingController.Push(child2);
//
//             // Act
//             PingController.Pop(child1);
//
//             // Assert
//             Assert.IsFalse(PingController.IsActive(child1)); // Child1 đã bị pop
//             Assert.IsTrue(PingController.IsActive(child2)); // Child2 vẫn active
//             Assert.IsTrue(PingController.IsActive(level2)); // Cấp 2 vẫn active vì còn child2
//             Assert.IsTrue(PingController.IsActive(level1)); // Cấp 1 vẫn active do có cấp 2
//         }
//
//         [Test]
//         public void ForceHide_AtDepth4_ShouldDisableAllRelevantNodes()
//         {
//             // Arrange
//             string level1 = "level1";
//             string level2 = "level1/level2";
//             string level3 = "level1/level2/level3";
//             string level4 = "level1/level2/level3/level4";
//
//             // Push tất cả các node
//             PingController.Push(level4);
//
//             // Act
//             PingController.Pop(level4, true); // Force hide level4
//
//             // Assert
//             Assert.IsFalse(PingController.IsActive(level4)); // Node cấp 4 bị vô hiệu hóa do force hide
//             Assert.IsFalse(PingController.IsActive(level3)); // Parent cấp 3 cũng bị vô hiệu hóa
//             Assert.IsFalse(PingController.IsActive(level2)); // Parent cấp 2 bị vô hiệu hóa
//             Assert.IsFalse(PingController.IsActive(level1)); // Parent cấp 1 bị vô hiệu hóa
//         }
//
//         [Test]
//         public void Push_AndPop_WithComplexTreeAtDepth4_ShouldHandleCorrectly()
//         {
//             // Arrange
//             string root = "root";
//             string child1 = "root/child1";
//             string child2 = "root/child2";
//             string subChild1 = "root/child1/subChild1";
//             string subChild2 = "root/child1/subChild2";
//
//             // Push toàn bộ cây
//             PingController.Push(subChild1);
//             PingController.Push(subChild2);
//             PingController.Push(child2);
//
//             // Assert trạng thái ban đầu
//             Assert.IsTrue(PingController.IsActive(root)); // Root phải active do có child
//             Assert.IsTrue(PingController.IsActive(child1)); // Child1 phải active vì có subChild1 và subChild2
//             Assert.IsTrue(PingController.IsActive(subChild1)); // subChild1 phải active
//             Assert.IsTrue(PingController.IsActive(subChild2)); // subChild2 phải active
//             Assert.IsTrue(PingController.IsActive(child2)); // Child2 phải active
//
//             // Act 1: Pop subChild1
//             PingController.Pop(subChild1);
//
//             // Assert sau khi Pop subChild1
//             Assert.IsFalse(PingController.IsActive(subChild1)); // subChild1 bị vô hiệu
//             Assert.IsTrue(PingController.IsActive(child1)); // Child1 vẫn active vì còn subChild2
//             Assert.IsTrue(PingController.IsActive(root)); // Root vẫn active do còn child1 và child2
//
//             // Act 2: Pop subChild2
//             PingController.Pop(subChild2);
//
//             // Assert sau khi Pop subChild2
//             Assert.IsFalse(PingController.IsActive(subChild2)); // subChild2 bị vô hiệu
//             Assert.IsFalse(PingController.IsActive(child1)); // Child1 bị vô hiệu vì không còn con nào
//             Assert.IsTrue(PingController.IsActive(child2)); // Child2 vẫn active
//             Assert.IsTrue(PingController.IsActive(root)); // Root vẫn active vì còn child2
//
//             // Act 3: Pop child2
//             PingController.Pop(child2);
//
//             // Assert sau khi Pop child2
//             Assert.IsFalse(PingController.IsActive(child2)); // child2 bị vô hiệu
//             Assert.IsFalse(PingController.IsActive(root)); // Root bị vô hiệu vì không còn con nào
//         }
//     }
// }